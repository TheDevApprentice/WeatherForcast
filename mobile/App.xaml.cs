using Microsoft.Extensions.Logging;
using mobile.Services.Theme;

namespace mobile
{
    public partial class App : Application
    {
        private readonly GlobalExceptionHandler _exceptionHandler;
        private readonly ILogger<App> _logger;
        private readonly IServiceProvider _serviceProvider;

        public App (
            GlobalExceptionHandler exceptionHandler,
            ILogger<App> logger,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _exceptionHandler = exceptionHandler;
            _logger = logger;
            _serviceProvider = serviceProvider;

            // L'overlay sera créé et enregistré dans CreateWindow

            // Démarrer la surveillance du réseau
            var networkMonitor = _serviceProvider.GetRequiredService<INetworkMonitorService>();
            networkMonitor.StartMonitoring();
#if DEBUG
            _logger.LogInformation("📡 NetworkMonitor démarré");
#endif

            // Initialiser le gestionnaire global d'exceptions
            _exceptionHandler.Initialize();

            // Initialiser le ConversationStore avec la conversation Support
            InitializeConversationStore();

            // Initialiser le cache SQLite en arrière-plan
            Task.Run(async () =>
            {
                try
                {
                    var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
                    await cacheService.InitializeAsync();
#if DEBUG
                    _logger.LogInformation("💾 Cache SQLite initialisé");
#endif
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de l'initialisation du cache");
                }
            });

#if DEBUG
            _logger.LogInformation("✅ Application démarrée");
#endif
        }

        /// <summary>
        /// Initialise le ConversationStore avec la conversation Support
        /// </summary>
        private void InitializeConversationStore()
        {
            try
            {
                var conversationStore = _serviceProvider.GetRequiredService<IConversationStore>();
                
                // TODO: Récupérer l'utilisateur actuel pour avoir son ID et nom
                // Pour l'instant, on utilise des valeurs par défaut
                conversationStore.Initialize("current-user", "Utilisateur");

#if DEBUG
                _logger.LogInformation("💬 ConversationStore initialisé avec conversation Support");
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du ConversationStore");
            }
        }

        protected override void OnSleep ()
        {
            base.OnSleep();
            _logger.LogInformation("💤 Application en arrière-plan");
            // Les animations seront automatiquement arrêtées via OnDisappearing des pages
        }

        protected override void OnResume ()
        {
            base.OnResume();
            _logger.LogInformation("▶️ Application reprise");
            // Les animations seront automatiquement redémarrées via OnAppearing des pages
        }

        protected override Window CreateWindow (IActivationState? activationState)
        {
            Shell shell;
            var networkMonitor = _serviceProvider.GetRequiredService<INetworkMonitorService>();
            var bannerManager = _serviceProvider.GetRequiredService<IOfflineBannerManager>();
            var themeService = _serviceProvider.GetRequiredService<IThemeService>();

#if ANDROID || IOS
            // Sur mobile : utiliser AppShellMobile avec TabBar
            var mobileShell = new AppShellMobile(bannerManager);
            shell = mobileShell;
            _logger.LogInformation("📱 AppShellMobile chargé (TabBar pour mobile)");
            
            // Initialiser le NetworkMonitor sur le Shell
            mobileShell.InitializeNetworkMonitor(networkMonitor);
#else
            // Sur desktop : utiliser AppShell avec Flyout
            var desktopShell = new AppShell(bannerManager, themeService);
            shell = desktopShell;
            
            // Désactiver le flyout pendant le splash
            shell.FlyoutBehavior = FlyoutBehavior.Disabled;
            Shell.SetFlyoutBehavior(shell, FlyoutBehavior.Disabled);
            shell.FlyoutIsPresented = false;
            
            _logger.LogInformation("🖥️ AppShell chargé (Flyout pour desktop)");
            
            // Initialiser le NetworkMonitor sur le Shell
            desktopShell.InitializeNetworkMonitor(networkMonitor);
#endif

#if WINDOWS || MACCATALYST
            // Utiliser MainWindow avec TitleBar personnalisée (Windows et Mac)
            var window = new MainWindow
            {
                Page = shell
            };

            // Masquer les éléments de la title bar AVANT la navigation vers le splash
            window.HideTitleBarElements();
            _logger.LogInformation("🔒 Éléments de la title bar masqués avant le splash");
#else
            // Utiliser Window standard (Android, iOS)
            var window = new Window(shell);
#endif

            // Créer et enregistrer l'overlay global pour les transitions de thème
            // L'overlay sera créé dans ThemeService lors de la première transition
            // Pour l'instant, on enregistre null et ThemeService créera l'overlay à la volée
            _logger.LogInformation("✅ ThemeService prêt pour les transitions de thème");

            // Naviguer vers la page de démarrage (Splash) qui gérera toutes les procédures
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 Démarrage de l'application");

#if ANDROID || IOS
                    // Sur mobile avec TabBar : masquer le TabBar et afficher Splash en modal
                    Shell.SetTabBarIsVisible(shell, false);
                    var splashPage = _serviceProvider.GetRequiredService<SplashPage>();
                    await shell.Navigation.PushModalAsync(splashPage, false);
#else
                    // Sur desktop avec Flyout : navigation globale vers splash
                    await shell.GoToAsync("///splash");
#endif
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la navigation vers SplashPage");
                    // En cas d'erreur, rediriger vers login par sécurité
#if ANDROID || IOS
                    var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
                    await shell.Navigation.PushModalAsync(loginPage, false);
#else
                    await shell.GoToAsync("///login");
#endif
                }
            });

            return window;
        }
    }
}
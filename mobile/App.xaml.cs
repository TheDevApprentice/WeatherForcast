using Microsoft.Extensions.Logging;
using mobile.Pages.Auth;
using mobile.Services.Internal.Interfaces;
using mobile.Services.Stores;
using mobile.Services.Theme;

namespace mobile
{
    public partial class App : Application
    {
        private readonly ILogger<App> _logger;
        private readonly IServiceProvider _serviceProvider;

        public App (
            ILogger<App> logger,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _logger = logger;
            _serviceProvider = serviceProvider;
            // L'overlay sera créé et enregistré dans CreateWindow

            // Démarrer la surveillance du réseau
            var networkMonitor = _serviceProvider.GetRequiredService<INetworkMonitorService>();
            networkMonitor.StartMonitoring();
#if DEBUG
            _logger.LogInformation("📡 NetworkMonitor démarré");
#endif

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
#if DEBUG
                    _logger.LogError(ex, "❌ Erreur lors de l'initialisation du cache");
#endif
                }
            });

#if DEBUG
            _logger.LogInformation("✅ Application démarrée");
#endif
        }

        /// <summary>
        /// Initialise le ConversationStore avec la conversation Support
        /// </summary>
        private void InitializeConversationStore ()
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
#if DEBUG
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du ConversationStore");
#endif
            }
        }

        protected override void OnSleep ()
        {
            base.OnSleep();
#if DEBUG
            _logger.LogInformation("💤 Application en arrière-plan");
#endif
            // Les animations seront automatiquement arrêtées via OnDisappearing des pages
        }

        protected override void OnResume ()
        {
            base.OnResume();
#if DEBUG
            _logger.LogInformation("▶️ Application reprise");
#endif
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
            var mobileShell = new AppShellMobile(bannerManager, networkMonitor);
            shell = mobileShell;
#if DEBUG
            _logger.LogInformation("📱 AppShellMobile chargé (TabBar pour mobile)");
#endif
#else
            // Sur desktop : utiliser AppShell avec Flyout
            var desktopShell = new AppShell(bannerManager, themeService, networkMonitor);
            shell = desktopShell;
            
            // Désactiver le flyout pendant le splash
            shell.FlyoutBehavior = FlyoutBehavior.Disabled;
            Shell.SetFlyoutBehavior(shell, FlyoutBehavior.Disabled);
            shell.FlyoutIsPresented = false;
#if DEBUG
            _logger.LogInformation("🖥️ AppShell chargé (Flyout pour desktop)");
#endif
#endif

#if WINDOWS || MACCATALYST
            // Utiliser MainWindow avec TitleBar personnalisée (Windows et Mac)
            var window = new MainWindow
            {
                Page = shell
            };

            // Masquer les éléments de la title bar AVANT la navigation vers le splash
            window.HideTitleBarElements();
#if DEBUG
            _logger.LogInformation("🔒 Éléments de la title bar masqués avant le splash");
#endif
#else
            // Utiliser Window standard (Android, iOS)
            var window = new Window(shell);
#endif

            // Créer et enregistrer l'overlay global pour les transitions de thème
            // L'overlay sera créé dans ThemeService lors de la première transition
            // Pour l'instant, on enregistre null et ThemeService créera l'overlay à la volée
#if DEBUG
            _logger.LogInformation("✅ ThemeService prêt pour les transitions de thème");
#endif
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
#if DEBUG
                    _logger.LogError(ex, "Erreur lors de la navigation vers SplashPage");
#endif
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
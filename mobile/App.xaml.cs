using mobile.Pages.Auth;
using mobile.Services.Internal.Interfaces;
using mobile.Services.Stores;

namespace mobile
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheService _cacheService;
        private readonly IConversationStore _conversationStore;

        public App (IServiceProvider serviceProvider, 
        INetworkMonitorService networkMonitor, 
        ICacheService cacheService, 
        IConversationStore conversationStore,
        IOfflineBannerManager bannerManager)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _cacheService = cacheService;
            _conversationStore = conversationStore;

            // Démarrer la surveillance du réseau
            networkMonitor.StartMonitoring();
            // NetworkMonitor démarré

            // Initialiser le ConversationStore avec la conversation Support
            _conversationStore.Initialize("current-user", "Utilisateur");

            // Initialiser le cache SQLite en arrière-plan
            Task.Run(async () =>
            {
                try
                {
                    await _cacheService.InitializeAsync();
                    // Cache SQLite initialisé
                }
                catch (Exception ex)
                {
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug App", $"❌ Erreur lors de l'initialisation du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }
            });

            // Application démarrée
        }

        protected override void OnSleep ()
        {
            base.OnSleep();
            // Application en arrière-plan
            // Les animations seront automatiquement arrêtées via OnDisappearing des pages
        }

        protected override void OnResume ()
        {
            base.OnResume();
            // Application reprise
            // Les animations seront automatiquement redémarrées via OnAppearing des pages
        }

        protected override Window CreateWindow (IActivationState? activationState)
        {
            Shell shell;

#if ANDROID || IOS
            // Sur mobile : récupérer AppShellMobile via DI
            shell = _serviceProvider.GetRequiredService<AppShellMobile>();
            // AppShellMobile chargé (TabBar pour mobile)
#else
            // Sur desktop : récupérer AppShell via DI
            shell = _serviceProvider.GetRequiredService<AppShell>();
            
            // Désactiver le flyout pendant le splash
            shell.FlyoutBehavior = FlyoutBehavior.Disabled;
            Shell.SetFlyoutBehavior(shell, FlyoutBehavior.Disabled);
            shell.FlyoutIsPresented = false;
            // AppShell chargé (Flyout pour desktop)
#endif

#if WINDOWS || MACCATALYST
            // Utiliser MainWindow avec TitleBar personnalisée (Windows et Mac)
            var window = new MainWindow
            {
                Page = shell
            };

            // Masquer les éléments de la title bar AVANT la navigation vers le splash
            window.HideTitleBarElements();
            // Éléments de la title bar masqués avant le splash
#else
            // Utiliser Window standard (Android, iOS)
            var window = new Window(shell);
#endif

            // Créer et enregistrer l'overlay global pour les transitions de thème
            // L'overlay sera créé dans ThemeService lors de la première transition
            // Pour l'instant, on enregistre null et ThemeService créera l'overlay à la volée
            // ThemeService prêt pour les transitions de thème
            // Naviguer vers la page de démarrage (Splash) qui gérera toutes les procédures
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Démarrage de l'application

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
                    await Shell.Current.DisplayAlert("Debug App", $"❌ Erreur lors de la navigation vers SplashPage: {ex.Message}\n{ex.GetType().Name}", "OK");
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
using Microsoft.Extensions.Logging;

namespace mobile
{
    public partial class App : Application
    {
        private readonly GlobalExceptionHandler _exceptionHandler;
        private readonly ILogger<App> _logger;
        private readonly IServiceProvider _serviceProvider;

        public App(
            GlobalExceptionHandler exceptionHandler,
            ILogger<App> logger,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _exceptionHandler = exceptionHandler;
            _logger = logger;
            _serviceProvider = serviceProvider;

            // Initialiser le gestionnaire global d'exceptions
            _exceptionHandler.Initialize();
            _logger.LogInformation("✅ Application démarrée");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Shell shell;

#if ANDROID || IOS
            // Sur mobile : utiliser AppShellMobile avec TabBar
            shell = new AppShellMobile();
            _logger.LogInformation("📱 AppShellMobile chargé (TabBar pour mobile)");
#else
            // Sur desktop : utiliser AppShell avec Flyout
            shell = new AppShell();
            
            // Désactiver le flyout pendant le splash
            shell.FlyoutBehavior = FlyoutBehavior.Disabled;
            Shell.SetFlyoutBehavior(shell, FlyoutBehavior.Disabled);
            shell.FlyoutIsPresented = false;
            
            _logger.LogInformation("🖥️ AppShell chargé (Flyout pour desktop)");
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

            // Naviguer vers la page de démarrage (Splash) qui gérera toutes les procédures
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 Démarrage de l'application");
                    
#if ANDROID || IOS
                    // Sur mobile avec TabBar : masquer le TabBar et afficher Splash en modal
                    Shell.SetTabBarIsVisible(shell, false);
                    var splashPage = _serviceProvider.GetRequiredService<Pages.SplashPage>();
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
                    var loginPage = _serviceProvider.GetRequiredService<Pages.Auth.LoginPage>();
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
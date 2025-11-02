using Microsoft.Extensions.Logging;

namespace mobile
{
    public partial class App : Application
    {
        private readonly GlobalExceptionHandler _exceptionHandler;
        private readonly ILogger<App> _logger;

        public App(
            GlobalExceptionHandler exceptionHandler,
            ILogger<App> logger)
        {
            InitializeComponent();
            _exceptionHandler = exceptionHandler;
            _logger = logger;

            // Initialiser le gestionnaire global d'exceptions
            _exceptionHandler.Initialize();
            _logger.LogInformation("✅ Application démarrée");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();

            // Naviguer vers la page de démarrage (Splash) qui gérera toutes les procédures
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 Démarrage de l'application");
                    await shell.GoToAsync("///splash");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la navigation vers SplashPage");
                    // En cas d'erreur, rediriger vers login par sécurité
                    await shell.GoToAsync("///login");
                }
            });

#if WINDOWS || MACCATALYST
            // Utiliser MainWindow avec TitleBar personnalisée (Windows et Mac)
            var window = new MainWindow
            {
                Page = shell
            };
#else
            // Utiliser Window standard (Android, iOS)
            var window = new Window(shell);
#endif

            return window;
        }
    }
}
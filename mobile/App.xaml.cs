using Microsoft.Extensions.Logging;

namespace mobile
{
    public partial class App : Application
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly GlobalExceptionHandler _exceptionHandler;
        private readonly ILogger<App> _logger;

        public App(
            ISecureStorageService secureStorage,
            GlobalExceptionHandler exceptionHandler,
            ILogger<App> logger)
        {
            InitializeComponent();
            _secureStorage = secureStorage;
            _exceptionHandler = exceptionHandler;
            _logger = logger;

            // Initialiser le gestionnaire global d'exceptions
            _exceptionHandler.Initialize();
            _logger.LogInformation("✅ Application démarrée avec gestion d'erreurs globale");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();

            // Vérifier l'authentification au démarrage
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var isAuthenticated = await _secureStorage.IsAuthenticatedAsync();

                // Mettre à jour l'UI du Shell selon l'état d'authentification
                shell.UpdateAuthenticationUI(isAuthenticated);

                // Naviguer vers la bonne page
                if (isAuthenticated)
                {
                    await shell.GoToAsync("///main");
                }
                else
                {
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
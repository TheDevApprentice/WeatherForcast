using Microsoft.Extensions.Logging;

namespace mobile
{
    public partial class App : Application
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly ISessionValidationService _sessionValidation;
        private readonly GlobalExceptionHandler _exceptionHandler;
        private readonly ILogger<App> _logger;

        public App(
            ISecureStorageService secureStorage,
            ISessionValidationService sessionValidation,
            GlobalExceptionHandler exceptionHandler,
            ILogger<App> logger)
        {
            InitializeComponent();
            _secureStorage = secureStorage;
            _sessionValidation = sessionValidation;
            _exceptionHandler = exceptionHandler;
            _logger = logger;

            // Initialiser le gestionnaire global d'exceptions
            _exceptionHandler.Initialize();
            _logger.LogInformation("✅ Application démarrée");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();

            // Vérifier l'authentification au démarrage avec validation de session
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    _logger.LogInformation("Vérification de l'authentification...");

                    // 1. Vérifier si un token existe (rapide, local)
                    var hasToken = await _secureStorage.IsAuthenticatedAsync();

                    if (!hasToken)
                    {
                        _logger.LogInformation("Aucun token, redirection vers login");
                        shell.UpdateAuthenticationUI(false);
                        await shell.GoToAsync("///login");
                        return;
                    }

                    // 2. Naviguer vers MainPage d'abord (UX fluide)
                    _logger.LogInformation("Token trouvé, navigation vers MainPage");
                    shell.UpdateAuthenticationUI(true);
                    await shell.GoToAsync("///main");

                    // 3. Valider la session en arrière-plan
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(500); // Laisser la page se charger

                            _logger.LogInformation("🔍 Début de la validation de la session en arrière-plan...");
                            var isValid = await _sessionValidation.ValidateSessionAsync();

                            _logger.LogInformation("🔍 Résultat de la validation: {IsValid}", isValid);

                            if (!isValid)
                            {
                                // Session invalide : nettoyer et rediriger
                                _logger.LogWarning("❌ Session invalide détectée, déconnexion en cours...");
                                await _sessionValidation.ClearSessionAsync();

                                _logger.LogInformation("🔄 Redirection vers la page de login...");
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    shell.UpdateAuthenticationUI(false);
                                    await shell.GoToAsync("///login");
                                    _logger.LogInformation("✅ Redirection effectuée");
                                });
                            }
                            else
                            {
                                _logger.LogInformation("✅ Session valide confirmée !");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Erreur lors de la validation de session en arrière-plan");
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la vérification de l'authentification");
                    shell.UpdateAuthenticationUI(false);
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
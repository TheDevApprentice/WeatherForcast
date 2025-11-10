using Microsoft.Extensions.Logging;
using mobile.Services.Notifications.Interfaces;

namespace mobile.Services.Handlers.ErrorHandling
{
    /// <summary>
    /// Gestionnaire global d'exceptions pour l'application mobile
    /// Capture toutes les exceptions non gérées et empêche les crashs
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private static GlobalExceptionHandler? _instance;

        public GlobalExceptionHandler (
            ILogger<GlobalExceptionHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
            _instance = this;
        }

        /// <summary>
        /// Initialise le gestionnaire global d'exceptions
        /// À appeler au démarrage de l'application
        /// </summary>
        public void Initialize ()
        {
            // Capturer les exceptions non gérées sur le thread principal
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Capturer les exceptions non gérées sur les tâches
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _logger.LogInformation("✅ GlobalExceptionHandler initialisé");
        }

        /// <summary>
        /// Gère les exceptions non gérées du domaine d'application
        /// </summary>
        private void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                _logger.LogCritical(ex, "❌ EXCEPTION NON GÉRÉE (AppDomain): {Message}", ex.Message);

                // Afficher l'erreur à l'utilisateur
                _ = HandleExceptionAsync(ex, "Erreur Critique", e.IsTerminating);
            }
        }

        /// <summary>
        /// Gère les exceptions non observées dans les tâches
        /// </summary>
        private void OnUnobservedTaskException (object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "❌ EXCEPTION NON OBSERVÉE (Task): {Message}", e.Exception.Message);

            // Marquer l'exception comme observée pour éviter le crash
            e.SetObserved();

            // Afficher l'erreur à l'utilisateur
            _ = HandleExceptionAsync(e.Exception, "Erreur Asynchrone", false);
        }

        /// <summary>
        /// Gère une exception de manière asynchrone
        /// </summary>
        private async Task HandleExceptionAsync (Exception ex, string title, bool isTerminating)
        {
            try
            {
                await _semaphore.WaitAsync();

                try
                {
                    // Extraire le message d'erreur utilisateur-friendly
                    var userMessage = GetUserFriendlyMessage(ex);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Application.Current?.MainPage != null)
                        {
                            var message = isTerminating
                                ? $"{userMessage}\n\nL'application va se fermer."
                                : userMessage;

                            await Application.Current.MainPage.DisplayAlert(
                                $"❌ {title}",
                                message,
                                "OK");
                        }
                    });
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception handlerEx)
            {
                // Si même le gestionnaire d'erreurs échoue, logger seulement
                _logger.LogCritical(handlerEx, "❌ Erreur dans le gestionnaire d'exceptions");
            }
        }

        /// <summary>
        /// Convertit une exception technique en message utilisateur-friendly
        /// </summary>
        private string GetUserFriendlyMessage (Exception ex)
        {
            return ex switch
            {
                HttpRequestException => "Impossible de se connecter au serveur. Vérifiez votre connexion Internet.",
                TaskCanceledException => "L'opération a pris trop de temps et a été annulée.",
                UnauthorizedAccessException => "Vous n'avez pas les permissions nécessaires pour cette action.",
                ArgumentNullException => "Une donnée requise est manquante.",
                InvalidOperationException => "Cette opération n'est pas possible dans l'état actuel.",
                NotSupportedException => "Cette fonctionnalité n'est pas supportée sur votre appareil.",
                _ => $"Une erreur inattendue s'est produite: {ex.Message}"
            };
        }

        /// <summary>
        /// Méthode statique pour gérer les exceptions depuis n'importe où
        /// </summary>
        public static async Task HandleAsync (Exception ex, string? context = null)
        {
            if (_instance != null)
            {
                _instance._logger.LogError(ex, "❌ Exception capturée: {Context} - {Message}", context ?? "Unknown", ex.Message);
                await _instance.HandleExceptionAsync(ex, context ?? "Erreur", false);
            }
        }
    }
}

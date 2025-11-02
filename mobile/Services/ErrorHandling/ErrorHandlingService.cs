using Microsoft.Extensions.Logging;
using mobile.Services.Exceptions;

namespace mobile.Services.ErrorHandling
{
    /// <summary>
    /// Service de gestion centralisée des erreurs
    /// Affiche les erreurs à l'utilisateur de manière appropriée
    /// </summary>
    public interface IErrorHandlingService
    {
        Task HandleErrorAsync(Exception exception, string? context = null);
        Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, string? context = null, T? defaultValue = default);
        Task ExecuteSafelyAsync(Func<Task> action, string? context = null);
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly INotificationService _notificationService;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public ErrorHandlingService(
            ILogger<ErrorHandlingService> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Gère une erreur de manière centralisée
        /// </summary>
        public async Task HandleErrorAsync(Exception exception, string? context = null)
        {
            await _semaphore.WaitAsync();

            try
            {
                // Logger l'erreur
                _logger.LogError(
                    exception,
                    "Erreur dans le contexte: {Context} - Type: {ExceptionType}",
                    context ?? "Unknown",
                    exception.GetType().Name);

                // Obtenir le message utilisateur
                var (title, message) = GetUserFriendlyMessage(exception);

                // Afficher à l'utilisateur
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Si c'est une erreur critique, afficher une alerte
                    if (exception is ServerException or NetworkException)
                    {
                        await _notificationService.ShowErrorAsync(message, title);
                    }
                    else if (exception is ValidationException)
                    {
                        await _notificationService.ShowWarningAsync(message, title);
                    }
                    else if (exception is AuthenticationException)
                    {
                        await _notificationService.ShowWarningAsync(message, title);
                        
                        // Rediriger vers la page de connexion
                        await Shell.Current.GoToAsync("///login");
                    }
                    else
                    {
                        await _notificationService.ShowErrorAsync(message, title);
                    }
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Exécute une action de manière sécurisée avec gestion d'erreurs automatique
        /// </summary>
        public async Task<T?> ExecuteSafelyAsync<T>(
            Func<Task<T>> action,
            string? context = null,
            T? defaultValue = default)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, context);
                return defaultValue;
            }
        }

        /// <summary>
        /// Exécute une action de manière sécurisée avec gestion d'erreurs automatique
        /// </summary>
        public async Task ExecuteSafelyAsync(Func<Task> action, string? context = null)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, context);
            }
        }

        /// <summary>
        /// Convertit une exception en message utilisateur-friendly
        /// </summary>
        private (string Title, string Message) GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                AppException appEx => (GetTitleForErrorCode(appEx.ErrorCode), appEx.UserMessage),
                
                HttpRequestException => (
                    "Erreur de connexion",
                    "Impossible de se connecter au serveur. Vérifiez votre connexion Internet."
                ),
                
                TaskCanceledException => (
                    "Timeout",
                    "L'opération a pris trop de temps et a été annulée. Veuillez réessayer."
                ),
                
                UnauthorizedAccessException => (
                    "Accès refusé",
                    "Vous n'avez pas les permissions nécessaires pour cette action."
                ),
                
                ArgumentNullException => (
                    "Données manquantes",
                    "Une donnée requise est manquante. Veuillez vérifier votre saisie."
                ),
                
                InvalidOperationException => (
                    "Opération invalide",
                    "Cette opération n'est pas possible dans l'état actuel."
                ),
                
                NotSupportedException => (
                    "Non supporté",
                    "Cette fonctionnalité n'est pas supportée sur votre appareil."
                ),
                
                _ => (
                    "Erreur inattendue",
                    $"Une erreur inattendue s'est produite: {exception.Message}"
                )
            };
        }

        /// <summary>
        /// Obtient un titre approprié selon le code d'erreur
        /// </summary>
        private string GetTitleForErrorCode(string? errorCode)
        {
            return errorCode switch
            {
                "NETWORK_ERROR" => "Erreur réseau",
                "AUTH_ERROR" => "Authentification requise",
                "AUTHORIZATION_ERROR" => "Accès refusé",
                "VALIDATION_ERROR" => "Données invalides",
                "SERVER_ERROR" => "Erreur serveur",
                "NOT_FOUND" => "Introuvable",
                "CONFLICT" => "Conflit",
                "RATE_LIMIT" => "Limite atteinte",
                _ => "Erreur"
            };
        }
    }
}

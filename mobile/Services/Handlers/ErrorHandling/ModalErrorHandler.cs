using Microsoft.Extensions.Logging;

namespace mobile.Services.Handlers.ErrorHandling
{
    /// <summary>
    /// Service de gestion des erreurs
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Gère une erreur et l'affiche à l'utilisateur
        /// </summary>
        /// <param name="ex">Exception à gérer</param>
        void HandleError (Exception ex);

        /// <summary>
        /// Gère une erreur de manière asynchrone
        /// </summary>
        /// <param name="ex">Exception à gérer</param>
        /// <param name="context">Contexte de l'erreur (optionnel)</param>
        Task HandleErrorAsync (Exception ex, string? context = null);

        /// <summary>
        /// Gère une erreur avec un message personnalisé
        /// </summary>
        /// <param name="ex">Exception à gérer</param>
        /// <param name="userMessage">Message à afficher à l'utilisateur</param>
        Task HandleErrorWithMessageAsync (Exception ex, string userMessage);

        /// <summary>
        /// Log une erreur sans afficher de message à l'utilisateur
        /// </summary>
        /// <param name="ex">Exception à logger</param>
        /// <param name="context">Contexte de l'erreur</param>
        void LogError (Exception ex, string? context = null);
    }

    /// <summary>
    /// Gestionnaire d'erreurs avec affichage modal
    /// </summary>
    public class ModalErrorHandler : IErrorHandler
    {
        private readonly ILogger<ModalErrorHandler> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public ModalErrorHandler (ILogger<ModalErrorHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gère une erreur et l'affiche à l'utilisateur (synchrone)
        /// </summary>
        public void HandleError (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur capturée: {Message}", ex.Message);
            // Fire and forget - ne pas attendre la tâche
            _ = HandleErrorAsync(ex);
        }

        /// <summary>
        /// Gère une erreur de manière asynchrone
        /// </summary>
        public async Task HandleErrorAsync (Exception ex, string? context = null)
        {
            _logger.LogError(ex, "❌ Erreur capturée ({Context}): {Message}", context ?? "Unknown", ex.Message);

            var userMessage = GetUserFriendlyMessage(ex);
            await DisplayAlertAsync("Erreur", userMessage);
        }

        /// <summary>
        /// Gère une erreur avec un message personnalisé
        /// </summary>
        public async Task HandleErrorWithMessageAsync (Exception ex, string userMessage)
        {
            _logger.LogError(ex, "❌ Erreur capturée: {Message}", ex.Message);
            await DisplayAlertAsync("Erreur", userMessage);
        }

        /// <summary>
        /// Log une erreur sans afficher de message
        /// </summary>
        public void LogError (Exception ex, string? context = null)
        {
            _logger.LogError(ex, "❌ Erreur loggée ({Context}): {Message}", context ?? "Unknown", ex.Message);
        }

        /// <summary>
        /// Affiche une alerte modale
        /// </summary>
        private async Task DisplayAlertAsync (string title, string message)
        {
            try
            {
                await _semaphore.WaitAsync();

                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert($"❌ {title}", message, "OK");
                        }
                        else if (Shell.Current is Shell shell)
                        {
                            await shell.DisplayAlert($"❌ {title}", message, "OK");
                        }
                    });
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception displayEx)
            {
                _logger.LogCritical(displayEx, "❌ Impossible d'afficher l'erreur à l'utilisateur");
            }
        }

        /// <summary>
        /// Convertit une exception en message utilisateur-friendly
        /// </summary>
        private string GetUserFriendlyMessage (Exception ex)
        {
            return ex switch
            {
                HttpRequestException => "Impossible de se connecter au serveur. Vérifiez votre connexion Internet.",
                TaskCanceledException => "L'opération a pris trop de temps et a été annulée.",
                UnauthorizedAccessException => "Vous n'avez pas les permissions nécessaires.",
                ArgumentNullException argEx => $"Donnée manquante: {argEx.ParamName}",
                InvalidOperationException => "Cette opération n'est pas possible actuellement.",
                NotSupportedException => "Cette fonctionnalité n'est pas supportée.",
                _ => $"Une erreur s'est produite: {ex.Message}"
            };
        }
    }
}
namespace mobile.Services
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
        void HandleError(Exception ex);

        /// <summary>
        /// Gère une erreur de manière asynchrone
        /// </summary>
        /// <param name="ex">Exception à gérer</param>
        /// <param name="context">Contexte de l'erreur (optionnel)</param>
        Task HandleErrorAsync(Exception ex, string? context = null);

        /// <summary>
        /// Gère une erreur avec un message personnalisé
        /// </summary>
        /// <param name="ex">Exception à gérer</param>
        /// <param name="userMessage">Message à afficher à l'utilisateur</param>
        Task HandleErrorWithMessageAsync(Exception ex, string userMessage);

        /// <summary>
        /// Log une erreur sans afficher de message à l'utilisateur
        /// </summary>
        /// <param name="ex">Exception à logger</param>
        /// <param name="context">Contexte de l'erreur</param>
        void LogError(Exception ex, string? context = null);
    }
}
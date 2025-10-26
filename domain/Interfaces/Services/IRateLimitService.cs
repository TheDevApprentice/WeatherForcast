namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service de rate limiting et protection contre les attaques
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Vérifier si une IP a dépassé la limite de requêtes
        /// </summary>
        Task<bool> IsRateLimitExceededAsync(string ipAddress, string endpoint, int maxRequests, TimeSpan window);

        /// <summary>
        /// Enregistrer une tentative de login échouée
        /// </summary>
        Task RecordFailedLoginAttemptAsync(string ipAddress, string email);

        /// <summary>
        /// Vérifier si une IP est temporairement bloquée (brute force)
        /// </summary>
        Task<bool> IsIpBlockedAsync(string ipAddress);

        /// <summary>
        /// Obtenir le temps restant avant déblocage
        /// </summary>
        Task<TimeSpan?> GetBlockTimeRemainingAsync(string ipAddress);

        /// <summary>
        /// Réinitialiser les tentatives échouées après un login réussi
        /// </summary>
        Task ResetFailedAttemptsAsync(string ipAddress);

        /// <summary>
        /// Bloquer manuellement une IP
        /// </summary>
        Task BlockIpAsync(string ipAddress, TimeSpan duration, string reason);

        /// <summary>
        /// Débloquer une IP
        /// </summary>
        Task UnblockIpAsync(string ipAddress);
    }
}

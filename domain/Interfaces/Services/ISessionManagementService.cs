using domain.Entities;

namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service de gestion du cycle de vie des sessions
    /// Responsabilité : CRUD sessions uniquement
    /// </summary>
    public interface ISessionManagementService
    {
        /// <summary>
        /// Créer une session Web (cookie)
        /// </summary>
        Task<Session> CreateWebSessionAsync(
            string userId,
            string cookieId,
            string? ipAddress = null,
            string? userAgent = null,
            int expirationDays = 7);

        /// <summary>
        /// Créer une session API (JWT)
        /// </summary>
        Task<Session> CreateApiSessionAsync(
            string userId,
            string jwtToken,
            string? ipAddress = null,
            string? userAgent = null,
            int expirationHours = 24);

        /// <summary>
        /// Révoquer une session spécifique
        /// </summary>
        Task<bool> RevokeAsync(Guid sessionId, string? reason = null, string? revokedBy = null);

        /// <summary>
        /// Révoquer toutes les sessions d'un utilisateur
        /// </summary>
        Task<int> RevokeAllByUserIdAsync(string userId);

        /// <summary>
        /// Supprimer une session
        /// </summary>
        Task<bool> DeleteAsync(Guid sessionId);

        /// <summary>
        /// Vérifier si une session est valide
        /// </summary>
        Task<bool> IsValidAsync(string token);

        /// <summary>
        /// Récupérer toutes les sessions actives d'un utilisateur
        /// </summary>
        Task<IEnumerable<Session>> GetActiveSessionsAsync(string userId);
    }
}

using domain.Entities;

namespace domain.Interfaces.Repos
{
    /// <summary>
    /// Repository pour gérer les sessions
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// Créer une nouvelle session
        /// </summary>
        Task<Session> CreateAsync(Session session);
        
        /// <summary>
        /// Créer une nouvelle session avec liaison utilisateur
        /// </summary>
        Task<Session> CreateSessionWithUserAsync(Session session, string userId);
        
        /// <summary>
        /// Récupérer une session par son token
        /// </summary>
        Task<Session?> GetByTokenAsync(string token);
        
        /// <summary>
        /// Récupérer une session par son ID
        /// </summary>
        Task<Session?> GetByIdAsync(Guid sessionId);
        
        /// <summary>
        /// Récupérer toutes les sessions actives d'un utilisateur
        /// </summary>
        Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(string userId);
        
        /// <summary>
        /// Révoquer une session (marque comme révoquée)
        /// </summary>
        Task<bool> RevokeAsync(Guid sessionId);
        
        /// <summary>
        /// Supprimer une session de la base de données (cascade delete UserSessions)
        /// </summary>
        Task<bool> DeleteAsync(Guid sessionId);
        
        /// <summary>
        /// Révoquer toutes les sessions d'un utilisateur
        /// </summary>
        Task<int> RevokeAllByUserIdAsync(string userId);
        
        /// <summary>
        /// Vérifier si une session est valide (non expirée et non révoquée)
        /// </summary>
        Task<bool> IsValidAsync(string token);
        
        /// <summary>
        /// Nettoyer les sessions expirées
        /// </summary>
        Task<int> CleanupExpiredSessionsAsync();
    }
}

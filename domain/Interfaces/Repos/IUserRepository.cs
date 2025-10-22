using domain.Entities;

namespace domain.Interfaces.Repos
{
    /// <summary>
    /// Repository pour gérer les utilisateurs
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Récupérer un utilisateur par ID
        /// </summary>
        Task<ApplicationUser?> GetByIdAsync(string userId);
        
        /// <summary>
        /// Récupérer un utilisateur par email
        /// </summary>
        Task<ApplicationUser?> GetByEmailAsync(string email);
        
        /// <summary>
        /// Récupérer tous les utilisateurs
        /// </summary>
        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        
        /// <summary>
        /// Mettre à jour la dernière connexion
        /// </summary>
        Task UpdateLastLoginAsync(string userId);
        
        /// <summary>
        /// Activer/Désactiver un compte
        /// </summary>
        Task<bool> SetActiveStatusAsync(string userId, bool isActive);
        
        /// <summary>
        /// Récupérer les sessions actives d'un utilisateur
        /// </summary>
        Task<IEnumerable<Session>> GetActiveSessionsAsync(string userId);
    }
}

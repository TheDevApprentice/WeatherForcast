using domain.Entities;

namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service de gestion du cycle de vie des utilisateurs
    /// Responsabilité : CRUD utilisateurs uniquement
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Créer un nouvel utilisateur
        /// </summary>
        Task<(bool Success, string[] Errors, ApplicationUser? User)> RegisterAsync(
            string email,
            string password,
            string firstName,
            string lastName);

        /// <summary>
        /// Récupérer un utilisateur par email
        /// </summary>
        Task<ApplicationUser?> GetByEmailAsync(string email);

        /// <summary>
        /// Récupérer un utilisateur par ID
        /// </summary>
        Task<ApplicationUser?> GetByIdAsync(string userId);

        /// <summary>
        /// Mettre à jour la date de dernière connexion
        /// </summary>
        Task UpdateLastLoginAsync(string userId);
    }
}

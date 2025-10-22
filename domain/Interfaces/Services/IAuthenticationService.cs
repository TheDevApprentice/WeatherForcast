using domain.Entities;

namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service d'orchestration de l'authentification
    /// Responsabilité : Coordonner UserManagement et SessionManagement pour Login/Register
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Valider les credentials d'un utilisateur
        /// </summary>
        Task<(bool Success, ApplicationUser? User)> ValidateCredentialsAsync(
            string email,
            string password);

        /// <summary>
        /// Inscription avec création de session automatique
        /// </summary>
        Task<(bool Success, string[] Errors, ApplicationUser? User)> RegisterWithSessionAsync(
            string email,
            string password,
            string firstName,
            string lastName,
            string sessionToken,
            string? ipAddress = null,
            string? userAgent = null,
            bool isApiSession = false,
            int expirationHours = 24);

        /// <summary>
        /// Connexion avec création de session automatique
        /// </summary>
        Task<(bool Success, ApplicationUser? User)> LoginWithSessionAsync(
            string email,
            string password,
            string sessionToken,
            string? ipAddress = null,
            string? userAgent = null,
            bool isApiSession = false,
            int expirationHours = 24);
    }
}

using domain.Entities;

namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service d'authentification (Register, Login)
    /// Centralisé pour Web et API
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Enregistrer un nouvel utilisateur
        /// </summary>
        Task<(bool Success, string[] Errors, ApplicationUser? User)> RegisterAsync(
            string email, 
            string password, 
            string firstName, 
            string lastName);

        /// <summary>
        /// Vérifier les credentials d'un utilisateur
        /// </summary>
        Task<(bool Success, ApplicationUser? User)> ValidateCredentialsAsync(string email, string password);

        /// <summary>
        /// Récupérer un utilisateur par email
        /// </summary>
        Task<ApplicationUser?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Récupérer un utilisateur par ID
        /// </summary>
        Task<ApplicationUser?> GetUserByIdAsync(string userId);

        /// <summary>
        /// Enregistrer un utilisateur ET créer sa session (opération complète)
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
        /// Login ET créer la session + mettre à jour LastLoginAt (opération complète)
        /// </summary>
        Task<(bool Success, ApplicationUser? User)> LoginWithSessionAsync(
            string email,
            string password,
            string sessionToken,
            string? ipAddress = null,
            string? userAgent = null,
            bool isApiSession = false,
            int expirationHours = 24);

        /// <summary>
        /// Créer une session Web pour un utilisateur déjà authentifié + mettre à jour LastLoginAt
        /// Utilisé par le Web après SignInManager.PasswordSignInAsync()
        /// </summary>
        Task CreateWebSessionWithLastLoginUpdateAsync(
            string userId,
            string sessionToken,
            string? ipAddress = null,
            string? userAgent = null,
            int expirationDays = 7);
        
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
        /// Révoquer une session spécifique (marque comme révoquée)
        /// </summary>
        Task<bool> RevokeSessionAsync(Guid sessionId);

        /// <summary>
        /// Supprimer une session de la base de données (cascade delete UserSessions)
        /// </summary>
        Task<bool> DeleteSessionAsync(Guid sessionId);

        /// <summary>
        /// Révoquer toutes les sessions d'un utilisateur
        /// </summary>
        Task<int> RevokeAllUserSessionsAsync(string userId);

        /// <summary>
        /// Vérifier si une session est valide
        /// </summary>
        Task<bool> IsSessionValidAsync(string token);

        /// <summary>
        /// Récupérer les sessions actives d'un utilisateur
        /// </summary>
        Task<IEnumerable<Session>> GetActiveSessionsAsync(string userId);
    }
}

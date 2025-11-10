namespace mobile.Services.Internal.Interfaces
{
    /// <summary>
    /// Interface pour le stockage sécurisé (JWT Token)
    /// </summary>
    public interface ISecureStorageService
    {
        Task SaveTokenAsync (string token);
        Task<string?> GetTokenAsync ();
        Task RemoveTokenAsync ();
        Task<bool> IsAuthenticatedAsync ();

        /// <summary>
        /// Vérifie si le token JWT est valide (non expiré)
        /// </summary>
        Task<bool> IsTokenValidAsync ();

        /// <summary>
        /// Extrait les informations utilisateur du token JWT (pour authentification offline)
        /// </summary>
        Task<(string UserId, string Email, string FirstName, string LastName)?> GetUserInfoFromTokenAsync ();

        Task SaveUserInfoAsync (string email, string firstName, string lastName);
        Task<(string Email, string FirstName, string LastName)> GetUserInfoAsync ();
        Task ClearAllAsync ();
    }
}

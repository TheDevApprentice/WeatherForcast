using mobile.Models;

namespace mobile.Services
{
    /// <summary>
    /// Service de gestion centralisée de l'état d'authentification
    /// Évite les vérifications redondantes en sauvegardant l'état en SecureStorage
    /// </summary>
    public interface IAuthenticationStateService
    {
        /// <summary>
        /// Récupère l'état d'authentification actuel
        /// </summary>
        Task<AuthenticationState> GetStateAsync();

        /// <summary>
        /// Sauvegarde l'état d'authentification
        /// </summary>
        Task SetStateAsync(AuthenticationState state);

        /// <summary>
        /// Efface l'état d'authentification
        /// </summary>
        Task ClearStateAsync();

        /// <summary>
        /// Vérifie si l'utilisateur est authentifié (lecture rapide du cache)
        /// </summary>
        Task<bool> IsAuthenticatedAsync();
    }
}

using mobile.Services.Api.Interfaces;
using mobile.Services.Internal.Interfaces;

namespace mobile.Services.Internal
{
    /// <summary>
    /// Service simple de validation de session au démarrage
    /// </summary>
    public class SessionValidationService : ISessionValidationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISecureStorageService _secureStorage;

        public SessionValidationService (
            IServiceProvider serviceProvider,
            ISecureStorageService secureStorage)
        {
            _serviceProvider = serviceProvider;
            _secureStorage = secureStorage;
        }

        /// <summary>
        /// Valide la session en appelant l'API /me
        /// Retourne true si la session est valide, false sinon
        /// </summary>
        public async Task<bool> ValidateSessionAsync ()
        {
            try
            {
                // Vérifier si un token existe
                var token = await _secureStorage.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    // Aucun token trouvé
                    return false;
                }

                // Vérifier que le token n'est pas expiré avant d'appeler l'API
                var isTokenValid = await _secureStorage.IsTokenValidAsync();
                if (!isTokenValid)
                {
                    // Token expiré, nettoyage de la session
                    await _secureStorage.ClearAllAsync();
                    return false;
                }

                // Token trouvé et valide, validation via API /me

                // Résoudre IApiAuthService via le ServiceProvider (évite le lifetime mismatch)
                using var scope = _serviceProvider.CreateScope();
                var apiAuthService = scope.ServiceProvider.GetRequiredService<IApiAuthService>();

                // Appeler l'API /me
                var currentUser = await apiAuthService.GetCurrentUserAsync();

                if (currentUser != null)
                {
                    // Session valide
                    return true;
                }

                // Session invalide: API /me n'a pas retourné d'utilisateur (401 Unauthorized)
                return false;
            }
            catch (HttpRequestException ex)
            {
                // Erreur réseau : impossible de valider, on déconnecte par sécurité
                return false;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SessionValidationService", $"❌ Erreur lors de la validation de session: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return false;
            }
        }

        /// <summary>
        /// Nettoie la session (supprime le token)
        /// </summary>
        public async Task ClearSessionAsync ()
        {
            try
            {
                // Nettoyage de la session
                await _secureStorage.ClearAllAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SessionValidationService", $"❌ Erreur lors du nettoyage de la session: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }
    }
}

using Microsoft.Extensions.Logging;

namespace mobile.Services
{
    /// <summary>
    /// Service simple de validation de session au démarrage
    /// </summary>
    public class SessionValidationService : ISessionValidationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISecureStorageService _secureStorage;
        private readonly ILogger<SessionValidationService> _logger;

        public SessionValidationService(
            IServiceProvider serviceProvider,
            ISecureStorageService secureStorage,
            ILogger<SessionValidationService> logger)
        {
            _serviceProvider = serviceProvider;
            _secureStorage = secureStorage;
            _logger = logger;
        }

        /// <summary>
        /// Valide la session en appelant l'API /me
        /// Retourne true si la session est valide, false sinon
        /// </summary>
        public async Task<bool> ValidateSessionAsync()
        {
            try
            {
                // Vérifier si un token existe
                var token = await _secureStorage.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Aucun token trouvé");
                    return false;
                }

                _logger.LogInformation("Token trouvé, validation via API /me...");

                // Résoudre IApiService via le ServiceProvider (évite le lifetime mismatch)
                using var scope = _serviceProvider.CreateScope();
                var apiService = scope.ServiceProvider.GetRequiredService<IApiService>();

                // Appeler l'API /me
                var currentUser = await apiService.GetCurrentUserAsync();

                if (currentUser != null)
                {
                    _logger.LogInformation("✅ Session valide pour: {Email}", currentUser.Email);
                    return true;
                }

                _logger.LogWarning("❌ Session invalide: API /me n'a pas retourné d'utilisateur (401 Unauthorized)");
                return false;
            }
            catch (HttpRequestException ex)
            {
                // Erreur réseau : impossible de valider, on déconnecte par sécurité
                _logger.LogError(ex, "❌ Erreur réseau lors de la validation, déconnexion par sécurité");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la validation de session");
                return false;
            }
        }

        /// <summary>
        /// Nettoie la session (supprime le token)
        /// </summary>
        public async Task ClearSessionAsync()
        {
            try
            {
                _logger.LogInformation("Nettoyage de la session...");
                await _secureStorage.ClearAllAsync();
                _logger.LogInformation("Session nettoyée");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage de la session");
            }
        }
    }
}

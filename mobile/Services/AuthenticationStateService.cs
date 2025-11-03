using Microsoft.Extensions.Logging;
using mobile.Models;
using System.Text.Json;

namespace mobile.Services
{
    /// <summary>
    /// Service de gestion centralisée de l'état d'authentification
    /// Sauvegarde l'état en SecureStorage pour éviter les vérifications redondantes
    /// </summary>
    public class AuthenticationStateService : IAuthenticationStateService
    {
        private const string AuthStateKey = "auth_state";
        private readonly ISecureStorageService _secureStorage;
        private readonly ILogger<AuthenticationStateService> _logger;
        private AuthenticationState? _cachedState;

        public AuthenticationStateService(
            ISecureStorageService secureStorage,
            ILogger<AuthenticationStateService> logger)
        {
            _secureStorage = secureStorage;
            _logger = logger;
        }

        /// <summary>
        /// Récupère l'état d'authentification (avec cache en mémoire)
        /// </summary>
        public async Task<AuthenticationState> GetStateAsync()
        {
            // Si déjà en cache, retourner directement
            if (_cachedState != null)
            {
                return _cachedState;
            }

            try
            {
                // Récupérer depuis SecureStorage
                var json = await SecureStorage.GetAsync(AuthStateKey);

                if (string.IsNullOrEmpty(json))
                {
                    _cachedState = AuthenticationState.Unauthenticated();
                    return _cachedState;
                }

                // Désérialiser
                _cachedState = JsonSerializer.Deserialize<AuthenticationState>(json);

                if (_cachedState == null)
                {
                    _cachedState = AuthenticationState.Unauthenticated();
                }

                return _cachedState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'état d'authentification");
                _cachedState = AuthenticationState.Unauthenticated();
                return _cachedState;
            }
        }

        /// <summary>
        /// Sauvegarde l'état d'authentification
        /// </summary>
        public async Task SetStateAsync(AuthenticationState state)
        {
            try
            {
                // Mettre à jour le cache
                _cachedState = state;

                // Sérialiser et sauvegarder
                var json = JsonSerializer.Serialize(state);
                await SecureStorage.SetAsync(AuthStateKey, json);

                _logger.LogInformation("État d'authentification sauvegardé: {IsAuth}", state.IsAuthenticated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde de l'état d'authentification");
            }
        }

        /// <summary>
        /// Efface l'état d'authentification
        /// </summary>
        public async Task ClearStateAsync()
        {
            try
            {
                _cachedState = null;
                SecureStorage.Remove(AuthStateKey);
                await Task.CompletedTask;

                _logger.LogInformation("État d'authentification effacé");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'effacement de l'état d'authentification");
            }
        }

        /// <summary>
        /// Vérifie rapidement si l'utilisateur est authentifié
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            var state = await GetStateAsync();
            return state.IsAuthenticated;
        }
    }
}

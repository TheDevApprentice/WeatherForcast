using Microsoft.Extensions.Logging;
using mobile.Services.Internal.Interfaces;
using System.Text.Json;

namespace mobile.Services.Internal
{
    /// <summary>
    /// Service de gestion centralisée de l'état d'authentification
    /// Sauvegarde l'état en SecureStorage pour éviter les vérifications redondantes
    /// </summary>
    public class AuthenticationStateService : IAuthenticationStateService
    {
        private const string AuthStateKey = "auth_state";
        private readonly ISecureStorageService _secureStorage;
        private AuthenticationState? _cachedState;

        public AuthenticationStateService (
            ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
        }

        /// <summary>
        /// Récupère l'état d'authentification (avec cache en mémoire)
        /// </summary>
        public async Task<AuthenticationState> GetStateAsync ()
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
#if DEBUG
                await Shell.Current.DisplayAlert("Debug AuthState", $"Erreur GetStateAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                _cachedState = AuthenticationState.Unauthenticated();
                return _cachedState;
            }
        }

        /// <summary>
        /// Sauvegarde l'état d'authentification
        /// </summary>
        public async Task SetStateAsync (AuthenticationState state)
        {
            try
            {
                // Mettre à jour le cache
                _cachedState = state;

                // Sérialiser et sauvegarder
                var json = JsonSerializer.Serialize(state);
                await SecureStorage.SetAsync(AuthStateKey, json);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug AuthState", $"Erreur SetStateAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Efface l'état d'authentification
        /// </summary>
        public async Task ClearStateAsync ()
        {
            try
            {
                _cachedState = null;
                SecureStorage.Remove(AuthStateKey);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug AuthState", $"Erreur ClearStateAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Vérifie rapidement si l'utilisateur est authentifié
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync ()
        {
            var state = await GetStateAsync();
            return state.IsAuthenticated;
        }
    }
}

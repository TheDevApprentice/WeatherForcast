using domain.DTOs.Auth;
using mobile.Services.Api.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace mobile.Services.Api
{
    /// <summary>
    /// Service pour les appels API d'authentification
    /// Responsabilité: Gestion de l'authentification et des utilisateurs
    /// </summary>
    public class ApiAuthService : IApiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiAuthService (HttpClient httpClient)
        {
            _httpClient = httpClient;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Authentifie un utilisateur avec email/password
        /// </summary>
        public async Task<AuthResponse?> LoginAsync (LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                    return authResponse;
                }

                return null;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiAuthService", $"❌ Erreur lors de la connexion: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                throw;
            }
        }

        /// <summary>
        /// Enregistre un nouvel utilisateur
        /// </summary>
        public async Task<bool> RegisterAsync (RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiAuthService", $"❌ Erreur lors de l'inscription: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return false;
            }
        }

        /// <summary>
        /// Valide le token JWT actuel
        /// </summary>
        public async Task<bool> ValidateTokenAsync ()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/auth/validate");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiAuthService", $"❌ Erreur lors de la validation du token: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return false;
            }
        }

        /// <summary>
        /// Récupère les informations de l'utilisateur connecté
        /// </summary>
        public async Task<AuthResponse?> GetCurrentUserAsync ()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/auth/me");

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                    return user;
                }
                return null;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiAuthService", $"❌ Erreur lors de la récupération de l'utilisateur: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                throw;
            }
        }

        /// <summary>
        /// Déconnecte l'utilisateur
        /// </summary>
        public async Task<bool> LogoutAsync ()
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/auth/logout", null);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiAuthService", $"❌ Erreur lors de la déconnexion: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return false;
            }
        }

        /// <summary>
        /// Vérifie si l'API est joignable
        /// Lève ApiUnavailableException si l'API n'est pas accessible (502, timeout, connexion refusée, etc.)
        /// Retourne true si l'API est joignable (même si le token est invalide - 401)
        /// Note: AuthenticatedHttpClientHandler gère déjà les retries et lève ApiUnavailableException
        /// </summary>
        public async Task<bool> CheckApiAvailabilityAsync ()
        {
            // Faire un appel simple - AuthenticatedHttpClientHandler gère les retries
            // et lève ApiUnavailableException si l'API est inaccessible
            var response = await _httpClient.GetAsync("/api/auth/me");

            // Si on arrive ici, l'API est joignable (même si 401)
            return true;
        }
    }
}

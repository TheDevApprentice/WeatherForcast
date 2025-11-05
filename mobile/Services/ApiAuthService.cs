using Microsoft.Extensions.Logging;
using mobile.Exceptions;
using mobile.Models.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace mobile.Services
{
    /// <summary>
    /// Service pour les appels API d'authentification
    /// Responsabilité: Gestion de l'authentification et des utilisateurs
    /// </summary>
    public class ApiAuthService : IApiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiAuthService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiAuthService(HttpClient httpClient, ILogger<ApiAuthService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Authentifie un utilisateur avec email/password
        /// </summary>
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
#if DEBUG
                _logger.LogDebug("🔐 Tentative de connexion pour {Email}", request.Email);
#endif

                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                    
#if DEBUG
                    _logger.LogDebug("✅ Connexion réussie pour {Email}", request.Email);
#endif
                    
                    return authResponse;
                }

#if DEBUG
                _logger.LogWarning("❌ Échec de connexion pour {Email}: {StatusCode}", 
                    request.Email, response.StatusCode);
#endif
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion pour {Email}", request.Email);
                throw;
            }
        }

        /// <summary>
        /// Enregistre un nouvel utilisateur
        /// </summary>
        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            try
            {
#if DEBUG
                _logger.LogDebug("📝 Tentative d'inscription pour {Email}", request.Email);
#endif

                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

                if (response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogDebug("✅ Inscription réussie pour {Email}", request.Email);
#endif
                    return true;
                }

#if DEBUG
                _logger.LogWarning("❌ Échec d'inscription pour {Email}: {StatusCode}", 
                    request.Email, response.StatusCode);
#endif
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'inscription pour {Email}", request.Email);
                return false;
            }
        }

        /// <summary>
        /// Valide le token JWT actuel
        /// </summary>
        public async Task<bool> ValidateTokenAsync()
        {
            try
            {
#if DEBUG
                _logger.LogDebug("🔍 Validation du token JWT");
#endif

                var response = await _httpClient.GetAsync("/api/auth/validate");
                if (response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogDebug("✅ Token valide");
#endif
                    return true;
                }

#if DEBUG
                _logger.LogWarning("❌ Token invalide: {StatusCode}", response.StatusCode);
#endif
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du token");
                return false;
            }
        }

        /// <summary>
        /// Récupère les informations de l'utilisateur connecté
        /// </summary>
        public async Task<CurrentUserResponse?> GetCurrentUserAsync()
        {
            try
            {
#if DEBUG
                _logger.LogDebug("👤 Récupération des informations utilisateur");
#endif

                var response = await _httpClient.GetAsync("/api/auth/me");

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(_jsonOptions);
                    
#if DEBUG
                    _logger.LogDebug("✅ Utilisateur récupéré: {Email}", user?.Email);
#endif
                    
                    return user;
                }

#if DEBUG
                _logger.LogWarning("❌ Échec récupération utilisateur: {StatusCode}", response.StatusCode);
#endif
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur");
                throw;
            }
        }

        /// <summary>
        /// Déconnecte l'utilisateur
        /// </summary>
        public async Task<bool> LogoutAsync()
        {
            try
            {
#if DEBUG
                _logger.LogDebug("🚺 Déconnexion utilisateur");
#endif

                var response = await _httpClient.PostAsync("/api/auth/logout", null);

                if (response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogDebug("✅ Déconnexion réussie");
#endif
                    return true;
                }

#if DEBUG
                _logger.LogWarning("❌ Échec déconnexion: {StatusCode}", response.StatusCode);
#endif
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si l'API est joignable
        /// Lève ApiUnavailableException si l'API n'est pas accessible (502, timeout, connexion refusée, etc.)
        /// Retourne true si l'API est joignable (même si le token est invalide - 401)
        /// Note: AuthenticatedHttpClientHandler gère déjà les retries et lève ApiUnavailableException
        /// </summary>
        public async Task<bool> CheckApiAvailabilityAsync()
        {
#if DEBUG
            _logger.LogDebug("🔍 Vérification disponibilité API...");
#endif

            // Faire un appel simple - AuthenticatedHttpClientHandler gère les retries
            // et lève ApiUnavailableException si l'API est inaccessible
            var response = await _httpClient.GetAsync("/api/auth/me");

            // Si on arrive ici, l'API est joignable (même si 401)
#if DEBUG
            _logger.LogDebug("✅ API joignable (Status: {StatusCode})", response.StatusCode);
#endif
            return true;
        }
    }
}

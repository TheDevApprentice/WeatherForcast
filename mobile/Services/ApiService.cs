using Microsoft.Extensions.Logging;
using mobile.Models;
using mobile.Models.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace mobile.Services
{
    /// <summary>
    /// Service pour les appels API REST
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        #region Authentification

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                }

                _logger.LogWarning("Échec de la connexion: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion");
                throw;
            }
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Inscription réussie pour {Email}", request.Email);
                    return true;
                }

                _logger.LogWarning("Échec de l'inscription: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'inscription");
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/auth/me");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du token");
                return false;
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/auth/logout", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion");
                return false;
            }
        }

        #endregion

        #region WeatherForecast

        public async Task<List<WeatherForecast>> GetForecastsAsync()
        {
            try
            {
                var forecasts = await _httpClient.GetFromJsonAsync<List<WeatherForecast>>(
                    "/api/weatherforecast", _jsonOptions);

                return forecasts ?? new List<WeatherForecast>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prévisions");
                throw;
            }
        }

        public async Task<WeatherForecast?> GetForecastByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<WeatherForecast>(
                    $"/api/weatherforecast/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la prévision {Id}", id);
                throw;
            }
        }

        public async Task<WeatherForecast?> CreateForecastAsync(CreateForecastRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/weatherforecast", request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<WeatherForecast>(_jsonOptions);
                }

                _logger.LogWarning("Échec de la création de prévision: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la prévision");
                throw;
            }
        }

        public async Task<bool> UpdateForecastAsync(int id, UpdateForecastRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/weatherforecast/{id}", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la prévision {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteForecastAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/weatherforecast/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la prévision {Id}", id);
                throw;
            }
        }

        #endregion
    }
}

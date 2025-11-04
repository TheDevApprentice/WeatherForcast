using Microsoft.Extensions.Logging;
using mobile.Models;
using mobile.Models.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace mobile.Services
{
    /// <summary>
    /// Service pour les appels API des prévisions météo
    /// Responsabilité: CRUD des prévisions météo
    /// </summary>
    public class ApiWeatherForecastService : IApiWeatherForecastService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiWeatherForecastService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiWeatherForecastService(HttpClient httpClient, ILogger<ApiWeatherForecastService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Récupère toutes les prévisions météo
        /// </summary>
        public async Task<List<WeatherForecast>> GetForecastsAsync()
        {
#if DEBUG
            _logger.LogDebug("☁️ Récupération de toutes les prévisions météo");
#endif

            var response = await _httpClient.GetAsync("/api/weatherforecast");

            if (response.IsSuccessStatusCode)
            {
                var forecasts = await response.Content.ReadFromJsonAsync<List<WeatherForecast>>(_jsonOptions);
                
#if DEBUG
                _logger.LogDebug("✅ {Count} prévisions récupérées", forecasts?.Count ?? 0);
#endif
                
                return forecasts ?? new List<WeatherForecast>();
            }

#if DEBUG
            _logger.LogWarning("❌ Échec récupération prévisions: {StatusCode}", response.StatusCode);
#endif
            return new List<WeatherForecast>();
        }

        /// <summary>
        /// Récupère une prévision par son ID
        /// </summary>
        public async Task<WeatherForecast?> GetForecastByIdAsync(int id)
        {
#if DEBUG
            _logger.LogDebug("🔍 Récupération de la prévision {Id}", id);
#endif

            var response = await _httpClient.GetAsync($"/api/weatherforecast/{id}");

            if (response.IsSuccessStatusCode)
            {
                var forecast = await response.Content.ReadFromJsonAsync<WeatherForecast>(_jsonOptions);
                
#if DEBUG
                _logger.LogDebug("✅ Prévision {Id} récupérée", id);
#endif
                
                return forecast;
            }

#if DEBUG
            _logger.LogWarning("❌ Échec récupération prévision {Id}: {StatusCode}", id, response.StatusCode);
#endif
            return null;
        }

        /// <summary>
        /// Crée une nouvelle prévision
        /// </summary>
        public async Task<WeatherForecast?> CreateForecastAsync(CreateForecastRequest request)
        {
#if DEBUG
            _logger.LogDebug("➕ Création d'une nouvelle prévision pour {Date}", request.Date);
#endif

            var response = await _httpClient.PostAsJsonAsync("/api/weatherforecast", request);

            if (response.IsSuccessStatusCode)
            {
                var forecast = await response.Content.ReadFromJsonAsync<WeatherForecast>(_jsonOptions);
                
#if DEBUG
                _logger.LogDebug("✅ Prévision créée avec ID {Id}", forecast?.Id);
#endif
                
                return forecast;
            }

#if DEBUG
            _logger.LogWarning("❌ Échec création prévision: {StatusCode}", response.StatusCode);
#endif
            return null;
        }

        /// <summary>
        /// Met à jour une prévision existante
        /// </summary>
        public async Task<bool> UpdateForecastAsync(int id, UpdateForecastRequest request)
        {
#if DEBUG
            _logger.LogDebug("✏️ Mise à jour de la prévision {Id}", id);
#endif

            var response = await _httpClient.PutAsJsonAsync($"/api/weatherforecast/{id}", request);

            if (response.IsSuccessStatusCode)
            {
#if DEBUG
                _logger.LogDebug("✅ Prévision {Id} mise à jour", id);
#endif
                return true;
            }

#if DEBUG
            _logger.LogWarning("❌ Échec mise à jour prévision {Id}: {StatusCode}", id, response.StatusCode);
#endif
            return false;
        }

        /// <summary>
        /// Supprime une prévision
        /// </summary>
        public async Task<bool> DeleteForecastAsync(int id)
        {
#if DEBUG
            _logger.LogDebug("🗑️ Suppression de la prévision {Id}", id);
#endif

            var response = await _httpClient.DeleteAsync($"/api/weatherforecast/{id}");

            if (response.IsSuccessStatusCode)
            {
#if DEBUG
                _logger.LogDebug("✅ Prévision {Id} supprimée", id);
#endif
                return true;
            }

#if DEBUG
            _logger.LogWarning("❌ Échec suppression prévision {Id}: {StatusCode}", id, response.StatusCode);
#endif
            return false;
        }
    }
}

using domain.DTOs.WeatherForecast;
using Microsoft.Extensions.Logging;
using mobile.Exceptions;
using mobile.Services.Api.Interfaces;
using mobile.Services.Internal.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace mobile.Services.Api
{
    /// <summary>
    /// Service pour les appels API des prévisions météo avec support du cache offline
    /// Pattern: Cache-Aside
    /// Stratégie: API-First avec fallback sur cache
    /// </summary>
    public class ApiWeatherForecastServiceWithCache : IApiWeatherForecastService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ApiWeatherForecastServiceWithCache> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // Durée de validité du cache (1 heure)
        private static readonly TimeSpan CacheValidity = TimeSpan.FromHours(1);

        public ApiWeatherForecastServiceWithCache (
            HttpClient httpClient,
            ICacheService cacheService,
            ILogger<ApiWeatherForecastServiceWithCache> logger)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Récupère toutes les prévisions météo
        /// Stratégie: API-First avec fallback sur cache
        /// </summary>
        public async Task<List<WeatherForecast>> GetForecastsAsync ()
        {
            try
            {
#if DEBUG
                _logger.LogDebug("☁️ Récupération des prévisions depuis l'API");
#endif

                // Essayer d'abord l'API
                var response = await _httpClient.GetAsync("/api/weatherforecast");

                if (!response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogWarning("⚠️ API a retourné {StatusCode}, tentative de récupération du cache", response.StatusCode);
#endif
                    return await GetFromCacheAsync();
                }

                var forecasts = await response.Content.ReadFromJsonAsync<List<WeatherForecast>>(_jsonOptions);

                if (forecasts != null && forecasts.Any())
                {
#if DEBUG
                    _logger.LogDebug("✅ {Count} prévisions récupérées de l'API", forecasts.Count);
#endif

                    // Sauvegarder dans le cache pour utilisation offline
                    try
                    {
                        await _cacheService.SaveForecastsAsync(forecasts);
#if DEBUG
                        _logger.LogDebug("💾 Prévisions sauvegardées dans le cache");
#endif
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "⚠️ Impossible de sauvegarder dans le cache");
                    }

                    return forecasts;
                }

#if DEBUG
                _logger.LogWarning("⚠️ API n'a retourné aucune prévision, tentative de récupération du cache");
#endif

                // Si l'API ne retourne rien, essayer le cache
                return await GetFromCacheAsync();
            }
            catch (HttpRequestException ex)
            {
                // Erreur réseau - Mode offline
                _logger.LogWarning(ex, "📡 Pas de connexion réseau - Mode offline activé");
                return await GetFromCacheAsync();
            }
            catch (Exception ex)
            {
                // Autre erreur - Essayer le cache
                _logger.LogError(ex, "❌ Erreur lors de la récupération des prévisions de l'API");
                return await GetFromCacheAsync();
            }
        }

        /// <summary>
        /// Récupère les prévisions du cache
        /// </summary>
        private async Task<List<WeatherForecast>> GetFromCacheAsync ()
        {
            try
            {
                var cachedForecasts = await _cacheService.GetCachedForecastsAsync();

                if (cachedForecasts != null && cachedForecasts.Any())
                {
                    // Vérifier si le cache est valide
                    var isCacheValid = await _cacheService.IsForecastsCacheValidAsync(CacheValidity);

                    if (isCacheValid)
                    {
#if DEBUG
                        _logger.LogDebug("✅ {Count} prévisions récupérées du cache (valide)", cachedForecasts.Count);
#endif
                    }
                    else
                    {
#if DEBUG
                        _logger.LogWarning("⚠️ {Count} prévisions récupérées du cache (expiré)", cachedForecasts.Count);
#endif
                    }

                    return cachedForecasts;
                }

#if DEBUG
                _logger.LogWarning("⚠️ Aucune prévision en cache");
#endif
                return new List<WeatherForecast>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération du cache");
                return new List<WeatherForecast>();
            }
        }

        /// <summary>
        /// Récupère une prévision par son ID
        /// Stratégie: API-First avec fallback sur cache
        /// </summary>
        public async Task<WeatherForecast?> GetForecastByIdAsync (int id)
        {
            try
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
                // Fallback sur le cache
                return await _cacheService.GetCachedForecastByIdAsync(id);
            }
            catch (ApiUnavailableException ex)
            {
                _logger.LogWarning(ex, "📡 API non joignable pour GetForecastByIdAsync({Id})", id);
                return await _cacheService.GetCachedForecastByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de GetForecastByIdAsync({Id})", id);
                return null;
            }
        }

        /// <summary>
        /// Crée une nouvelle prévision
        /// Stratégie: API-Only (nécessite connexion)
        /// </summary>
        public async Task<WeatherForecast?> CreateForecastAsync (CreateWeatherForecastRequest request)
        {
            try
            {
#if DEBUG
                _logger.LogDebug("➕ Création d'une nouvelle prévision pour {Date}", request.Date);
#endif

                var response = await _httpClient.PostAsJsonAsync("/api/weatherforecast", request);

                if (!response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogWarning("❌ Échec création prévision: {StatusCode}", response.StatusCode);
#endif
                    return null;
                }

                var forecast = await response.Content.ReadFromJsonAsync<WeatherForecast>(_jsonOptions);

#if DEBUG
                _logger.LogDebug("✅ Prévision créée avec ID {Id}", forecast?.Id);
#endif

                if (forecast != null)
                {
                    // Invalider le cache pour forcer un refresh
                    try
                    {
                        await _cacheService.ClearForecastsCacheAsync();
#if DEBUG
                        _logger.LogDebug("🗑️ Cache invalidé après création");
#endif
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "⚠️ Impossible d'invalider le cache");
                    }
                }

                return forecast;
            }
            catch (ApiUnavailableException ex)
            {
                _logger.LogWarning(ex, "📡 Impossible de créer une prévision en mode offline");
                throw new InvalidOperationException("La création de prévisions nécessite une connexion internet", ex);
            }
        }

        /// <summary>
        /// Met à jour une prévision existante
        /// Stratégie: API-Only (nécessite connexion)
        /// </summary>
        public async Task<bool> UpdateForecastAsync (int id, UpdateWeatherForecastRequest request)
        {
            try
            {
#if DEBUG
                _logger.LogDebug("✏️ Mise à jour de la prévision {Id}", id);
#endif

                var response = await _httpClient.PutAsJsonAsync($"/api/weatherforecast/{id}", request);

                if (!response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogWarning("❌ Échec mise à jour prévision {Id}: {StatusCode}", id, response.StatusCode);
#endif
                    return false;
                }

#if DEBUG
                _logger.LogDebug("✅ Prévision {Id} mise à jour", id);
#endif
                var success = true;

                if (success)
                {
                    // Invalider le cache pour forcer un refresh
                    try
                    {
                        await _cacheService.ClearForecastsCacheAsync();
#if DEBUG
                        _logger.LogDebug("🗑️ Cache invalidé après mise à jour");
#endif
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "⚠️ Impossible d'invalider le cache");
                    }
                }

                return success;
            }
            catch (ApiUnavailableException ex)
            {
                _logger.LogWarning(ex, "📡 Impossible de mettre à jour une prévision en mode offline");
                throw new InvalidOperationException("La mise à jour de prévisions nécessite une connexion internet", ex);
            }
        }

        /// <summary>
        /// Supprime une prévision
        /// Stratégie: API-Only (nécessite connexion)
        /// </summary>
        public async Task<bool> DeleteForecastAsync (int id)
        {
            try
            {
#if DEBUG
                _logger.LogDebug("🗑️ Suppression de la prévision {Id}", id);
#endif

                var response = await _httpClient.DeleteAsync($"/api/weatherforecast/{id}");

                if (!response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogWarning("❌ Échec suppression prévision {Id}: {StatusCode}", id, response.StatusCode);
#endif
                    return false;
                }

#if DEBUG
                _logger.LogDebug("✅ Prévision {Id} supprimée", id);
#endif
                var success = true;

                if (success)
                {
                    // Supprimer aussi du cache
                    try
                    {
                        await _cacheService.DeleteCachedForecastAsync(id);
#if DEBUG
                        _logger.LogDebug("🗑️ Prévision {Id} supprimée du cache", id);
#endif
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "⚠️ Impossible de supprimer du cache");
                    }
                }

                return success;
            }
            catch (ApiUnavailableException ex)
            {
                _logger.LogWarning(ex, "📡 Impossible de supprimer une prévision en mode offline");
                throw new InvalidOperationException("La suppression de prévisions nécessite une connexion internet", ex);
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using mobile.Models;
using mobile.Models.DTOs;

namespace mobile.Services
{
    /// <summary>
    /// Decorator pour ApiWeatherForecastService avec support du cache offline
    /// Pattern: Decorator + Cache-Aside
    /// Stratégie: Cache-First avec fallback sur API
    /// </summary>
    public class ApiWeatherForecastServiceWithCache : IApiWeatherForecastService
    {
        private readonly IApiWeatherForecastService _innerService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ApiWeatherForecastServiceWithCache> _logger;

        // Durée de validité du cache (1 heure)
        private static readonly TimeSpan CacheValidity = TimeSpan.FromHours(1);

        public ApiWeatherForecastServiceWithCache(
            IApiWeatherForecastService innerService,
            ICacheService cacheService,
            ILogger<ApiWeatherForecastServiceWithCache> logger)
        {
            _innerService = innerService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les prévisions météo
        /// Stratégie: Cache-First avec fallback sur API
        /// </summary>
        public async Task<List<WeatherForecast>> GetForecastsAsync()
        {
            try
            {
#if DEBUG
                _logger.LogDebug("🔍 Tentative de récupération des prévisions depuis l'API");
#endif

                // Essayer d'abord l'API
                var forecasts = await _innerService.GetForecastsAsync();

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
        private async Task<List<WeatherForecast>> GetFromCacheAsync()
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
        /// Stratégie: API-First (pas de cache pour les requêtes individuelles)
        /// </summary>
        public async Task<WeatherForecast?> GetForecastByIdAsync(int id)
        {
            try
            {
                return await _innerService.GetForecastByIdAsync(id);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "📡 Pas de connexion réseau pour GetForecastByIdAsync({Id})", id);
                
                // Fallback sur le cache
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
        public async Task<WeatherForecast?> CreateForecastAsync(CreateForecastRequest request)
        {
            try
            {
                var forecast = await _innerService.CreateForecastAsync(request);

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
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "📡 Impossible de créer une prévision en mode offline");
                throw new InvalidOperationException("La création de prévisions nécessite une connexion internet", ex);
            }
        }

        /// <summary>
        /// Met à jour une prévision existante
        /// Stratégie: API-Only (nécessite connexion)
        /// </summary>
        public async Task<bool> UpdateForecastAsync(int id, UpdateForecastRequest request)
        {
            try
            {
                var success = await _innerService.UpdateForecastAsync(id, request);

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
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "📡 Impossible de mettre à jour une prévision en mode offline");
                throw new InvalidOperationException("La mise à jour de prévisions nécessite une connexion internet", ex);
            }
        }

        /// <summary>
        /// Supprime une prévision
        /// Stratégie: API-Only (nécessite connexion)
        /// </summary>
        public async Task<bool> DeleteForecastAsync(int id)
        {
            try
            {
                var success = await _innerService.DeleteForecastAsync(id);

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
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "📡 Impossible de supprimer une prévision en mode offline");
                throw new InvalidOperationException("La suppression de prévisions nécessite une connexion internet", ex);
            }
        }
    }
}

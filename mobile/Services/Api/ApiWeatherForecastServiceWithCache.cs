using domain.DTOs.WeatherForecast;
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
        private readonly JsonSerializerOptions _jsonOptions;

        // Durée de validité du cache (1 heure)
        private static readonly TimeSpan CacheValidity = TimeSpan.FromHours(1);

        public ApiWeatherForecastServiceWithCache (
            HttpClient httpClient,
            ICacheService cacheService)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;

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
                // Récupération des prévisions depuis l'API
                // Essayer d'abord l'API
                var response = await _httpClient.GetAsync("/api/weatherforecast");

                if (!response.IsSuccessStatusCode)
                {
                    // API a retourné un code d'erreur, tentative de récupération du cache
                    return await GetFromCacheAsync();
                }

                var forecasts = await response.Content.ReadFromJsonAsync<List<WeatherForecast>>(_jsonOptions);

                if (forecasts != null && forecasts.Any())
                {
                    // Prévisions récupérées de l'API avec succès
                    // Sauvegarder dans le cache pour utilisation offline
                    try
                    {
                        await _cacheService.SaveForecastsAsync(forecasts);
                        // Prévisions sauvegardées dans le cache
                    }
                    catch (Exception cacheEx)
                    {
                        // Impossible de sauvegarder dans le cache (non bloquant)
                    }

                    return forecasts;
                }

                // API n'a retourné aucune prévision, tentative de récupération du cache
                // Si l'API ne retourne rien, essayer le cache
                return await GetFromCacheAsync();
            }
            catch (HttpRequestException ex)
            {
                // Pas de connexion réseau - Mode offline activé
                return await GetFromCacheAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiWeatherForecastService", $"❌ Erreur lors de la récupération des prévisions de l'API: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                        // Prévisions récupérées du cache (valide)
                    }
                    else
                    {
                        // Prévisions récupérées du cache (expiré)
                    }

                    return cachedForecasts;
                }

                // Aucune prévision en cache
                return new List<WeatherForecast>();
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiWeatherForecastService", $"❌ Erreur lors de la récupération du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                // Récupération de la prévision par ID depuis l'API
                var response = await _httpClient.GetAsync($"/api/weatherforecast/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var forecast = await response.Content.ReadFromJsonAsync<WeatherForecast>(_jsonOptions);
                    // Prévision récupérée avec succès
                    return forecast;
                }

                // Échec récupération prévision, fallback sur le cache
                return await _cacheService.GetCachedForecastByIdAsync(id);
            }
            catch (ApiUnavailableException ex)
            {
                // API non joignable, fallback sur le cache
                return await _cacheService.GetCachedForecastByIdAsync(id);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ApiWeatherForecastService", $"❌ Erreur lors de GetForecastByIdAsync({id}): {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                // Création d'une nouvelle prévision
                var response = await _httpClient.PostAsJsonAsync("/api/weatherforecast", request);

                if (!response.IsSuccessStatusCode)
                {
                    // Échec création prévision
                    return null;
                }

                var forecast = await response.Content.ReadFromJsonAsync<WeatherForecast>(_jsonOptions);
                // Prévision créée avec succès

                if (forecast != null)
                {
                    // Invalider le cache pour forcer un refresh
                    try
                    {
                        await _cacheService.ClearForecastsCacheAsync();
                        // Cache invalidé après création
                    }
                    catch (Exception cacheEx)
                    {
                        // Impossible d'invalider le cache (non bloquant)
                    }
                }

                return forecast;
            }
            catch (ApiUnavailableException ex)
            {
                // Impossible de créer une prévision en mode offline
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
                // Mise à jour de la prévision
                var response = await _httpClient.PutAsJsonAsync($"/api/weatherforecast/{id}", request);

                if (!response.IsSuccessStatusCode)
                {
                    // Échec mise à jour prévision
                    return false;
                }

                // Prévision mise à jour avec succès
                var success = true;

                if (success)
                {
                    // Invalider le cache pour forcer un refresh
                    try
                    {
                        await _cacheService.ClearForecastsCacheAsync();
                        // Cache invalidé après mise à jour
                    }
                    catch (Exception cacheEx)
                    {
                        // Impossible d'invalider le cache (non bloquant)
                    }
                }

                return success;
            }
            catch (ApiUnavailableException ex)
            {
                // Impossible de mettre à jour une prévision en mode offline
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
                // Suppression de la prévision
                var response = await _httpClient.DeleteAsync($"/api/weatherforecast/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    // Échec suppression prévision
                    return false;
                }

                // Prévision supprimée avec succès
                var success = true;

                if (success)
                {
                    // Supprimer aussi du cache
                    try
                    {
                        await _cacheService.DeleteCachedForecastAsync(id);
                        // Prévision supprimée du cache
                    }
                    catch (Exception cacheEx)
                    {
                        // Impossible de supprimer du cache (non bloquant)
                    }
                }

                return success;
            }
            catch (ApiUnavailableException ex)
            {
                // Impossible de supprimer une prévision en mode offline
                throw new InvalidOperationException("La suppression de prévisions nécessite une connexion internet", ex);
            }
        }
    }
}

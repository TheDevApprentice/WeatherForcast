namespace mobile.Services
{
    /// <summary>
    /// Interface pour le service de cache local (SQLite)
    /// Responsabilité: Stockage et récupération des données en cache pour le mode offline
    /// </summary>
    public interface ICacheService
    {
        #region Forecasts Cache

        /// <summary>
        /// Sauvegarde une liste de prévisions dans le cache
        /// </summary>
        Task SaveForecastsAsync(IEnumerable<Models.WeatherForecast> forecasts);

        /// <summary>
        /// Récupère toutes les prévisions du cache
        /// </summary>
        Task<List<Models.WeatherForecast>> GetCachedForecastsAsync();

        /// <summary>
        /// Récupère une prévision du cache par son ID
        /// </summary>
        Task<Models.WeatherForecast?> GetCachedForecastByIdAsync(int id);

        /// <summary>
        /// Supprime une prévision du cache
        /// </summary>
        Task DeleteCachedForecastAsync(int id);

        /// <summary>
        /// Vide tout le cache des prévisions
        /// </summary>
        Task ClearForecastsCacheAsync();

        /// <summary>
        /// Vérifie si le cache des prévisions est valide (pas trop ancien)
        /// </summary>
        Task<bool> IsForecastsCacheValidAsync(TimeSpan maxAge);

        #endregion

        #region Profiles Cache

        /// <summary>
        /// Sauvegarde un profil dans le cache
        /// </summary>
        Task SaveProfileAsync(string email, string firstName, string lastName);

        /// <summary>
        /// Récupère tous les profils du cache
        /// </summary>
        Task<List<Models.SavedUserProfile>> GetCachedProfilesAsync();

        /// <summary>
        /// Supprime un profil du cache
        /// </summary>
        Task DeleteCachedProfileAsync(string email);

        /// <summary>
        /// Vide tout le cache des profils
        /// </summary>
        Task ClearProfilesCacheAsync();

        #endregion

        #region General Cache Operations

        /// <summary>
        /// Vide tout le cache (forecasts + profiles)
        /// </summary>
        Task ClearAllCacheAsync();

        /// <summary>
        /// Récupère la taille totale du cache en octets
        /// </summary>
        Task<long> GetCacheSizeAsync();

        /// <summary>
        /// Initialise la base de données du cache
        /// </summary>
        Task InitializeAsync();

        #endregion
    }
}

using mobile.Models.Cache;
using mobile.Services.Internal.Interfaces;
using SQLite;

namespace mobile.Services.Internal
{
    /// <summary>
    /// Service de cache local utilisant SQLite
    /// Responsabilité: Stockage et récupération des données en cache pour le mode offline
    /// </summary>
    public class CacheService : ICacheService
    {
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _isInitialized = false;

        // Durée de validité par défaut du cache (1 heure)
        private static readonly TimeSpan DefaultCacheValidity = TimeSpan.FromHours(1);

        public CacheService ()
        {
        }

        #region Initialization

        /// <summary>
        /// Initialise la base de données SQLite
        /// </summary>
        public async Task InitializeAsync ()
        {
            if (_isInitialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "weatherforecast_cache.db");

                _database = new SQLiteAsyncConnection(dbPath);

                // Créer les tables
                await _database.CreateTableAsync<CachedForecast>();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors de l'initialisation du cache SQLite: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task EnsureInitializedAsync ()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }

        #endregion

        #region Forecasts Cache

        /// <summary>
        /// Sauvegarde une liste de prévisions dans le cache
        /// </summary>
        public async Task SaveForecastsAsync (IEnumerable<WeatherForecast> forecasts)
        {
            await EnsureInitializedAsync();

            try
            {
                var cachedForecasts = forecasts.Select(f => CachedForecast.FromWeatherForecast(f)).ToList();

                // Supprimer les anciennes prévisions et insérer les nouvelles
                await _database!.RunInTransactionAsync(db =>
                {
                    db.DeleteAll<CachedForecast>();
                    db.InsertAll(cachedForecasts);
                });
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors de la sauvegarde des prévisions dans le cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                throw;
            }
        }

        /// <summary>
        /// Récupère toutes les prévisions du cache
        /// </summary>
        public async Task<List<WeatherForecast>> GetCachedForecastsAsync ()
        {
            await EnsureInitializedAsync();

            try
            {
                var cachedForecasts = await _database!.Table<CachedForecast>()
                    .OrderByDescending(f => f.Date)
                    .ToListAsync();

                var forecasts = cachedForecasts
                    .Select(cf => cf.ToWeatherForecast())
                    .ToList();

                return forecasts;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors de la récupération des prévisions du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return new List<WeatherForecast>();
            }
        }

        /// <summary>
        /// Récupère une prévision du cache par son ID
        /// </summary>
        public async Task<WeatherForecast?> GetCachedForecastByIdAsync (int id)
        {
            await EnsureInitializedAsync();

            try
            {
                var cachedForecast = await _database!.Table<CachedForecast>()
                    .Where(f => f.Id == id)
                    .FirstOrDefaultAsync();

                if (cachedForecast != null)
                {
                    return cachedForecast.ToWeatherForecast();
                }

                return null;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors de la récupération de la prévision du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return null;
            }
        }

        /// <summary>
        /// Supprime une prévision du cache
        /// </summary>
        public async Task DeleteCachedForecastAsync (int id)
        {
            await EnsureInitializedAsync();

            try
            {
                await _database!.Table<CachedForecast>()
                    .DeleteAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors de la suppression de la prévision du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Vide tout le cache des prévisions
        /// </summary>
        public async Task ClearForecastsCacheAsync ()
        {
            await EnsureInitializedAsync();

            try
            {
                await _database!.DeleteAllAsync<CachedForecast>();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors du vidage du cache des prévisions: {ex.Message}\n{ex.GetType().Name}", "OK");
            }
        }

        /// <summary>
        /// Vérifie si le cache des prévisions est valide (pas trop ancien)
        /// </summary>
        public async Task<bool> IsForecastsCacheValidAsync (TimeSpan maxAge)
        {
            await EnsureInitializedAsync();

            try
            {
                var oldestAllowed = DateTime.UtcNow - maxAge;

                var cachedForecast = await _database!.Table<CachedForecast>()
                    .OrderBy(f => f.CachedAt)
                    .FirstOrDefaultAsync();

                if (cachedForecast == null)
                {
                    return false;
                }

                var isValid = cachedForecast.CachedAt >= oldestAllowed;

                return isValid;
            }
            catch (Exception ex)
            {
#if DEBUG                
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors de la vérification de la validité du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return false;
            }
        }

        #endregion

        #region Profiles Cache

        /// <summary>
        /// Sauvegarde un profil dans le cache
        /// Note: Les profils sont gérés par SavedProfilesService via SecureStorage
        /// Cette méthode est un placeholder pour compatibilité future
        /// </summary>
        public Task SaveProfileAsync (string email, string firstName, string lastName)
        {
            // Les profils sont actuellement gérés par SavedProfilesService
            // Cette méthode pourrait être implémentée plus tard si on veut migrer vers SQLite

            return Task.CompletedTask;
        }

        /// <summary>
        /// Récupère tous les profils du cache
        /// Note: Les profils sont gérés par SavedProfilesService via SecureStorage
        /// </summary>
        public Task<List<SavedUserProfile>> GetCachedProfilesAsync ()
        {
            // Les profils sont actuellement gérés par SavedProfilesService
            return Task.FromResult(new List<SavedUserProfile>());
        }

        /// <summary>
        /// Supprime un profil du cache
        /// </summary>
        public Task DeleteCachedProfileAsync (string email)
        {
            // Les profils sont actuellement gérés par SavedProfilesService
            return Task.CompletedTask;
        }

        /// <summary>
        /// Vide tout le cache des profils
        /// </summary>
        public Task ClearProfilesCacheAsync ()
        {
            // Les profils sont actuellement gérés par SavedProfilesService
            return Task.CompletedTask;
        }

        #endregion

        #region General Cache Operations

        /// <summary>
        /// Vide tout le cache (forecasts + profiles)
        /// </summary>
        public async Task ClearAllCacheAsync ()
        {
            await EnsureInitializedAsync();

            try
            {
                await ClearForecastsCacheAsync();
                await ClearProfilesCacheAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors du vidage complet du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Récupère la taille totale du cache en octets
        /// </summary>
        public async Task<long> GetCacheSizeAsync ()
        {
            await EnsureInitializedAsync();

            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "weatherforecast_cache.db");

                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    var sizeInBytes = fileInfo.Length;

                    return sizeInBytes;
                }

                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug CacheService", $"❌ Erreur lors de la récupération de la taille du cache: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return 0;
            }
        }

        #endregion
    }
}

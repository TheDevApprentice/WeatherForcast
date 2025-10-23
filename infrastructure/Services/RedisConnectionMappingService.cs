using Microsoft.Extensions.Caching.Distributed;
using shared.Services;

namespace infrastructure.Services
{
    /// <summary>
    /// Implémentation Redis du service de mapping des connexions SignalR
    /// Stocke les mappings userId → connectionId dans Redis pour partage entre instances
    /// </summary>
    public class RedisConnectionMappingService : IConnectionMappingService
    {
        private readonly IDistributedCache _cache;
        private const string KeyPrefix = "signalr:user:";
        private const string KeySuffix = ":connectionId";

        public RedisConnectionMappingService(IDistributedCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Ajouter un mapping userId → connectionId dans Redis
        /// </summary>
        public async Task AddConnectionAsync(string userId, string connectionId)
        {
            var key = $"{KeyPrefix}{userId}{KeySuffix}";
            
            await _cache.SetStringAsync(
                key,
                connectionId,
                new DistributedCacheEntryOptions
                {
                    // Expiration après 24h d'inactivité
                    SlidingExpiration = TimeSpan.FromHours(24)
                });
        }

        /// <summary>
        /// Récupérer le ConnectionId d'un utilisateur depuis Redis
        /// </summary>
        public async Task<string?> GetConnectionIdAsync(string userId)
        {
            var key = $"{KeyPrefix}{userId}{KeySuffix}";
            return await _cache.GetStringAsync(key);
        }

        /// <summary>
        /// Retirer un mapping userId → connectionId de Redis
        /// </summary>
        public async Task RemoveConnectionAsync(string userId, string connectionId)
        {
            var key = $"{KeyPrefix}{userId}{KeySuffix}";
            await _cache.RemoveAsync(key);
        }
    }
}

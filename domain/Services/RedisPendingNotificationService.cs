using domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace domain.Services
{
    public class RedisPendingNotificationService : IPendingNotificationService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisPendingNotificationService> _logger;
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(2);

        private record Item(string Type, string PayloadJson, DateTime Utc);

        public RedisPendingNotificationService(IDistributedCache cache, ILogger<RedisPendingNotificationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        private static string Key(string channel, string key) => $"pending:{channel}:{key.ToLowerInvariant()}";

        public async Task AddAsync(string channel, string keyId, string type, string payloadJson, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = Key(channel, keyId);
                var existing = await _cache.GetStringAsync(key, cancellationToken);
                var list = string.IsNullOrEmpty(existing)
                    ? new List<Item>()
                    : (JsonSerializer.Deserialize<List<Item>>(existing) ?? new List<Item>());

                list.Add(new Item(type, payloadJson, DateTime.UtcNow));

                var serialized = JsonSerializer.Serialize(list);
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl };
                await _cache.SetStringAsync(key, serialized, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'une notification en attente pour {Channel}:{Key}", channel, keyId);
            }
        }

        public async Task<IReadOnlyList<(string Type, string PayloadJson)>> FetchPendingAsync(string channel, string keyId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = Key(channel, keyId);
                var existing = await _cache.GetStringAsync(key, cancellationToken);
                if (string.IsNullOrEmpty(existing)) return Array.Empty<(string, string)>();

                var list = JsonSerializer.Deserialize<List<Item>>(existing) ?? new List<Item>();

                // Clear key after fetch
                await _cache.RemoveAsync(key, cancellationToken);

                return list.Select(i => (i.Type, i.PayloadJson)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des notifications en attente pour {Channel}:{Key}", channel, keyId);
                return Array.Empty<(string, string)>();
            }
        }
    }
}

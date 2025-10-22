using domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace domain.Services
{
    /// <summary>
    /// Service de rate limiting avec cache en mémoire
    /// Pour production: utiliser Redis pour distribution multi-serveurs
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitService> _logger;

        // Configuration brute force
        private const int MAX_FAILED_ATTEMPTS = 5;
        private const int BLOCK_DURATION_MINUTES = 15;

        public RateLimitService(
            IMemoryCache cache,
            ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<bool> IsRateLimitExceededAsync(
            string ipAddress,
            string endpoint,
            int maxRequests,
            TimeSpan window)
        {
            var key = $"ratelimit:{ipAddress}:{endpoint}";

            if (!_cache.TryGetValue(key, out int requestCount))
            {
                // Première requête dans la fenêtre
                _cache.Set(key, 1, window);
                return Task.FromResult(false);
            }

            if (requestCount >= maxRequests)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for IP {IpAddress} on {Endpoint}. Count: {Count}/{Max}",
                    ipAddress, endpoint, requestCount, maxRequests);
                return Task.FromResult(true);
            }

            // Incrémenter le compteur
            _cache.Set(key, requestCount + 1, window);
            return Task.FromResult(false);
        }

        public Task RecordFailedLoginAttemptAsync(string ipAddress, string email)
        {
            var key = $"failedlogin:{ipAddress}";

            if (!_cache.TryGetValue(key, out int failedAttempts))
            {
                failedAttempts = 0;
            }

            failedAttempts++;
            _cache.Set(key, failedAttempts, TimeSpan.FromMinutes(30));

            _logger.LogWarning(
                "Failed login attempt {Attempt}/{Max} from IP {IpAddress} for email {Email}",
                failedAttempts, MAX_FAILED_ATTEMPTS, ipAddress, email);

            // Bloquer l'IP si trop de tentatives
            if (failedAttempts >= MAX_FAILED_ATTEMPTS)
            {
                var blockKey = $"blocked:{ipAddress}";
                var blockUntil = DateTime.UtcNow.AddMinutes(BLOCK_DURATION_MINUTES);
                _cache.Set(blockKey, blockUntil, TimeSpan.FromMinutes(BLOCK_DURATION_MINUTES));

                _logger.LogError(
                    "IP {IpAddress} blocked for {Duration} minutes due to {Attempts} failed login attempts",
                    ipAddress, BLOCK_DURATION_MINUTES, failedAttempts);
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsIpBlockedAsync(string ipAddress)
        {
            var key = $"blocked:{ipAddress}";

            if (_cache.TryGetValue(key, out DateTime blockUntil))
            {
                if (DateTime.UtcNow < blockUntil)
                {
                    return Task.FromResult(true);
                }

                // Le blocage a expiré, nettoyer
                _cache.Remove(key);
            }

            return Task.FromResult(false);
        }

        public Task<TimeSpan?> GetBlockTimeRemainingAsync(string ipAddress)
        {
            var key = $"blocked:{ipAddress}";

            if (_cache.TryGetValue(key, out DateTime blockUntil))
            {
                var remaining = blockUntil - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    return Task.FromResult<TimeSpan?>(remaining);
                }
            }

            return Task.FromResult<TimeSpan?>(null);
        }

        public Task ResetFailedAttemptsAsync(string ipAddress)
        {
            var key = $"failedlogin:{ipAddress}";
            _cache.Remove(key);

            _logger.LogInformation("Reset failed login attempts for IP {IpAddress}", ipAddress);

            return Task.CompletedTask;
        }

        public Task BlockIpAsync(string ipAddress, TimeSpan duration, string reason)
        {
            var key = $"blocked:{ipAddress}";
            var blockUntil = DateTime.UtcNow.Add(duration);
            _cache.Set(key, blockUntil, duration);

            _logger.LogWarning(
                "IP {IpAddress} manually blocked for {Duration}. Reason: {Reason}",
                ipAddress, duration, reason);

            return Task.CompletedTask;
        }

        public Task UnblockIpAsync(string ipAddress)
        {
            var blockKey = $"blocked:{ipAddress}";
            var failedKey = $"failedlogin:{ipAddress}";

            _cache.Remove(blockKey);
            _cache.Remove(failedKey);

            _logger.LogInformation("IP {IpAddress} unblocked", ipAddress);

            return Task.CompletedTask;
        }
    }
}

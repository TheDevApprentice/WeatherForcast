using domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace domain.Services
{
    /// <summary>
    /// Service de rate limiting avec Redis (IDistributedCache)
    /// Support multi-serveurs et clustering
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RateLimitService> _logger;

        // Configuration brute force
        private const int MAX_FAILED_ATTEMPTS = 5;
        private const int BLOCK_DURATION_MINUTES = 15;

        public RateLimitService(
            IDistributedCache cache,
            ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsRateLimitExceededAsync(
            string ipAddress,
            string endpoint,
            int maxRequests,
            TimeSpan window)
        {
            var key = $"ratelimit:{ipAddress}:{endpoint}";

            var countStr = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(countStr))
            {
                // Première requête dans la fenêtre
                await _cache.SetStringAsync(key, "1", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = window
                });
                return false;
            }

            var requestCount = int.Parse(countStr);

            if (requestCount >= maxRequests)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for IP {IpAddress} on {Endpoint}. Count: {Count}/{Max}",
                    ipAddress, endpoint, requestCount, maxRequests);
                return true;
            }

            // Incrémenter le compteur
            await _cache.SetStringAsync(key, (requestCount + 1).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window
            });
            return false;
        }

        public async Task RecordFailedLoginAttemptAsync(string ipAddress, string email)
        {
            var key = $"failedlogin:{ipAddress}";

            var countStr = await _cache.GetStringAsync(key);
            var failedAttempts = string.IsNullOrEmpty(countStr) ? 0 : int.Parse(countStr);

            failedAttempts++;
            await _cache.SetStringAsync(key, failedAttempts.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            _logger.LogWarning(
                "Failed login attempt {Attempt}/{Max} from IP {IpAddress} for email {Email}",
                failedAttempts, MAX_FAILED_ATTEMPTS, ipAddress, email);

            // Bloquer l'IP si trop de tentatives
            if (failedAttempts >= MAX_FAILED_ATTEMPTS)
            {
                var blockKey = $"blocked:{ipAddress}";
                var blockUntil = DateTime.UtcNow.AddMinutes(BLOCK_DURATION_MINUTES);
                await _cache.SetStringAsync(blockKey, blockUntil.ToString("O"), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BLOCK_DURATION_MINUTES)
                });

                _logger.LogError(
                    "IP {IpAddress} blocked for {Duration} minutes due to {Attempts} failed login attempts",
                    ipAddress, BLOCK_DURATION_MINUTES, failedAttempts);
            }
        }

        public async Task<bool> IsIpBlockedAsync(string ipAddress)
        {
            var key = $"blocked:{ipAddress}";

            var blockUntilStr = await _cache.GetStringAsync(key);
            
            if (!string.IsNullOrEmpty(blockUntilStr))
            {
                var blockUntil = DateTime.Parse(blockUntilStr);
                
                if (DateTime.UtcNow < blockUntil)
                {
                    return true;
                }

                // Le blocage a expiré, nettoyer
                await _cache.RemoveAsync(key);
            }

            return false;
        }

        public async Task<TimeSpan?> GetBlockTimeRemainingAsync(string ipAddress)
        {
            var key = $"blocked:{ipAddress}";

            var blockUntilStr = await _cache.GetStringAsync(key);
            
            if (!string.IsNullOrEmpty(blockUntilStr))
            {
                var blockUntil = DateTime.Parse(blockUntilStr);
                var remaining = blockUntil - DateTime.UtcNow;
                
                if (remaining > TimeSpan.Zero)
                {
                    return remaining;
                }
            }

            return null;
        }

        public async Task ResetFailedAttemptsAsync(string ipAddress)
        {
            var key = $"failedlogin:{ipAddress}";
            await _cache.RemoveAsync(key);

            _logger.LogInformation("Reset failed login attempts for IP {IpAddress}", ipAddress);
        }

        public async Task BlockIpAsync(string ipAddress, TimeSpan duration, string reason)
        {
            var key = $"blocked:{ipAddress}";
            var blockUntil = DateTime.UtcNow.Add(duration);
            await _cache.SetStringAsync(key, blockUntil.ToString("O"), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            });

            _logger.LogWarning(
                "IP {IpAddress} manually blocked for {Duration}. Reason: {Reason}",
                ipAddress, duration, reason);
        }

        public async Task UnblockIpAsync(string ipAddress)
        {
            var blockKey = $"blocked:{ipAddress}";
            var failedKey = $"failedlogin:{ipAddress}";

            await _cache.RemoveAsync(blockKey);
            await _cache.RemoveAsync(failedKey);

            _logger.LogInformation("IP {IpAddress} unblocked", ipAddress);
        }
    }
}

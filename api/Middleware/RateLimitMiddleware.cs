using domain.Interfaces.Services;

namespace api.Middleware
{
    /// <summary>
    /// Middleware de rate limiting pour l'API REST
    /// Limite le nombre de requêtes par IP
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;

        // Configuration: 60 requêtes par minute par IP pour l'API
        private const int MAX_REQUESTS_PER_MINUTE = 60;
        private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(1);

        public RateLimitMiddleware(
            RequestDelegate next,
            ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IRateLimitService rateLimitService)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var endpoint = context.Request.Path.ToString();

            // Vérifier si l'IP est bloquée (brute force)
            if (await rateLimitService.IsIpBlockedAsync(ipAddress))
            {
                var timeRemaining = await rateLimitService.GetBlockTimeRemainingAsync(ipAddress);
                var minutes = (int)(timeRemaining?.TotalMinutes ?? 0);

                _logger.LogWarning("Blocked IP {IpAddress} attempted to access {Endpoint}", ipAddress, endpoint);

                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "IP_BLOCKED",
                    Message = $"Your IP has been temporarily blocked due to suspicious activity. Try again in {minutes} minute(s).",
                    BlockedUntil = DateTime.UtcNow.Add(timeRemaining ?? TimeSpan.Zero)
                });
                return;
            }

            // Vérifier le rate limit
            if (await rateLimitService.IsRateLimitExceededAsync(
                ipAddress, 
                endpoint, 
                MAX_REQUESTS_PER_MINUTE, 
                WINDOW))
            {
                _logger.LogWarning("Rate limit exceeded for IP {IpAddress} on {Endpoint}", ipAddress, endpoint);

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers["Retry-After"] = "60"; // Réessayer dans 60 secondes
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "RATE_LIMIT_EXCEEDED",
                    Message = "Too many requests. Please try again later.",
                    RetryAfter = 60
                });
                return;
            }

            await _next(context);
        }
    }
}

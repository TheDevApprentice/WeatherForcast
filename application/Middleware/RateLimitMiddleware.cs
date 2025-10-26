using domain.Interfaces.Services;

namespace application.Middleware
{
    /// <summary>
    /// Middleware de rate limiting pour l'application Web MVC
    /// Limite le nombre de requêtes par IP
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;

        // Configuration: 100 requêtes par minute par IP
        private const int MAX_REQUESTS_PER_MINUTE = 100;
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
                await context.Response.WriteAsync(
                    $"Votre adresse IP a été temporairement bloquée en raison de trop nombreuses tentatives. " +
                    $"Réessayez dans {minutes} minute(s).");
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
                await context.Response.WriteAsync(
                    "Trop de requêtes. Veuillez réessayer dans quelques instants.");
                return;
            }

            await _next(context);
        }
    }
}

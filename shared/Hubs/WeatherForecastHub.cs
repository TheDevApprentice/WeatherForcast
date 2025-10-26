using domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace shared.Hubs
{
    /// <summary>
    /// Hub SignalR pour les notifications en temps réel des prévisions météo
    /// </summary>
    [Authorize]
    public class WeatherForecastHub : Hub
    {
        private readonly ILogger<WeatherForecastHub> _logger;
        private readonly IConnectionMappingService _connectionMapping;

        public WeatherForecastHub(
            ILogger<WeatherForecastHub> logger,
            IConnectionMappingService connectionMapping)
        {
            _logger = logger;
            _connectionMapping = connectionMapping;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var connectionId = Context.ConnectionId;

            _logger.LogInformation(
                "Client connecté au WeatherForecastHub: {UserName} (UserId: {UserId}, ConnectionId: {ConnectionId})",
                userName, userId, connectionId);

            // Stocker le mapping userId → connectionId dans Redis
            if (!string.IsNullOrEmpty(userId))
            {
                await _connectionMapping.AddConnectionAsync(userId, connectionId);
                _logger.LogDebug("Mapping stocké: UserId {UserId} → ConnectionId {ConnectionId}", userId, connectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var connectionId = Context.ConnectionId;

            _logger.LogInformation(
                "Client déconnecté du WeatherForecastHub: {UserName} (UserId: {UserId}, ConnectionId: {ConnectionId})",
                userName, userId, connectionId);

            // Retirer le mapping userId → connectionId de Redis
            if (!string.IsNullOrEmpty(userId))
            {
                await _connectionMapping.RemoveConnectionAsync(userId, connectionId);
                _logger.LogDebug("Mapping supprimé: UserId {UserId} → ConnectionId {ConnectionId}", userId, connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}

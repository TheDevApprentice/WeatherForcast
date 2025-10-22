using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace application.Hubs
{
    /// <summary>
    /// Hub SignalR pour les notifications en temps réel des prévisions météo
    /// </summary>
    [Authorize]
    public class WeatherForecastHub : Hub
    {
        private readonly ILogger<WeatherForecastHub> _logger;

        public WeatherForecastHub(ILogger<WeatherForecastHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("Client connecté au WeatherForecastHub: {UserName} ({ConnectionId})", 
                userName, Context.ConnectionId);
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("Client déconnecté du WeatherForecastHub: {UserName} ({ConnectionId})", 
                userName, Context.ConnectionId);
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}

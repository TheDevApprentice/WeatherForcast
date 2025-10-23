using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace shared.Hubs
{
    /// <summary>
    /// Hub SignalR dédié aux notifications admin en temps réel
    /// Seuls les utilisateurs avec le rôle Admin peuvent se connecter
    /// Utilisé pour monitorer les activités : users, sessions, API keys, etc.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminHub : Hub
    {
        private readonly ILogger<AdminHub> _logger;

        public AdminHub(ILogger<AdminHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            var connectionId = Context.ConnectionId;
            
            _logger.LogInformation(
                "🔐 Admin {UserName} connecté au AdminHub (ConnectionId: {ConnectionId})",
                userName, connectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            var connectionId = Context.ConnectionId;
            
            _logger.LogInformation(
                "🔐 Admin {UserName} déconnecté du AdminHub (ConnectionId: {ConnectionId})",
                userName, connectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}

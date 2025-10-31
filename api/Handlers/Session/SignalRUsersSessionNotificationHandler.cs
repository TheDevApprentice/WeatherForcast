using domain.Events;
using domain.Events.Admin;
using Microsoft.AspNetCore.SignalR;
using shared.Hubs;

namespace api.Handlers.Session
{
    /// <summary>
    /// Handler qui notifie l'utilisateur concerné via SignalR quand sa session est révoquée
    /// Force la déconnexion immédiate de l'utilisateur
    /// </summary>
    public class SignalRUsersSessionNotificationHandler :
        INotificationHandler<SessionRevokedEvent>
    {
        private readonly IHubContext<UsersHub> _usersHubContext;
        private readonly ILogger<SignalRUsersSessionNotificationHandler> _logger;

        public SignalRUsersSessionNotificationHandler(
            IHubContext<UsersHub> usersHubContext,
            ILogger<SignalRUsersSessionNotificationHandler> logger)
        {
            _usersHubContext = usersHubContext;
            _logger = logger;
        }

        /// <summary>
        /// Gère l'événement de révocation de session pour notifier l'utilisateur concerné
        /// </summary>
        public async Task Handle(SessionRevokedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "API - [UsersHub] Forcing logout for user {Email} - Session {SessionId} revoked by {RevokedBy}",
                notification.Email,
                notification.SessionId,
                notification.RevokedBy ?? "System");

            try
            {
                // Notifier l'utilisateur spécifique que sa session a été révoquée
                // Utiliser l'UserId comme groupe pour cibler l'utilisateur
                await _usersHubContext.Clients.Group($"User_{notification.UserId}").SendAsync(
                    "SessionRevoked",
                    new
                    {
                        notification.SessionId,
                        notification.RevokedAt,
                        notification.Reason,
                        notification.RevokedBy,
                        Message = "Votre session a été révoquée par un administrateur. Vous allez être déconnecté."
                    },
                    cancellationToken);

                // Forcer la déconnexion immédiate
                await _usersHubContext.Clients.Group($"User_{notification.UserId}").SendAsync(
                    "ForceLogout",
                    new
                    {
                        Reason = notification.Reason ?? "Session révoquée par l'administrateur",
                        RedirectUrl = "/Auth/Login"
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "✅ API - [UsersHub] Logout notification sent to user {Email}",
                    notification.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ API - [UsersHub] Erreur lors de la notification de révocation de session pour {Email}",
                    notification.Email);
            }
        }
    }
}

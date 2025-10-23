using domain.Events.Admin;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using shared.Hubs;

namespace application.Handlers.Admin
{
    /// <summary>
    /// Handler qui broadcaste les événements admin via SignalR
    /// Permet aux admins de voir en temps réel les activités : users, sessions, API keys, etc.
    /// </summary>
    public class SignalRAdminNotificationHandler :
        INotificationHandler<UserRegisteredEvent>,
        INotificationHandler<UserLoggedInEvent>,
        INotificationHandler<UserLoggedOutEvent>,
        INotificationHandler<SessionCreatedEvent>,
        INotificationHandler<ApiKeyCreatedEvent>,
        INotificationHandler<ApiKeyRevokedEvent>,
        INotificationHandler<UserRoleChangedEvent>,
        INotificationHandler<UserClaimChangedEvent>
    {
        private readonly IHubContext<AdminHub> _adminHubContext;
        private readonly ILogger<SignalRAdminNotificationHandler> _logger;

        public SignalRAdminNotificationHandler(
            IHubContext<AdminHub> adminHubContext,
            ILogger<SignalRAdminNotificationHandler> logger)
        {
            _adminHubContext = adminHubContext;
            _logger = logger;
        }

        /// <summary>
        /// Gère l'événement d'enregistrement d'un nouvel utilisateur
        /// </summary>
        public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting UserRegistered: {Email} from {IpAddress}",
                notification.Email,
                notification.IpAddress ?? "Unknown");

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "UserRegistered",
                    new
                    {
                        notification.UserId,
                        notification.Email,
                        notification.UserName,
                        notification.RegisteredAt,
                        notification.IpAddress
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (UserRegistered)");
            }
        }

        /// <summary>
        /// Gère l'événement de connexion d'un utilisateur
        /// </summary>
        public async Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting UserLoggedIn: {Email} from {IpAddress}",
                notification.Email,
                notification.IpAddress ?? "Unknown");

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "UserLoggedIn",
                    new
                    {
                        notification.UserId,
                        notification.Email,
                        notification.UserName,
                        notification.LoggedInAt,
                        notification.IpAddress,
                        notification.UserAgent
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (UserLoggedIn)");
            }
        }

        /// <summary>
        /// Gère l'événement de déconnexion d'un utilisateur
        /// </summary>
        public async Task Handle(UserLoggedOutEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting UserLoggedOut: {Email}",
                notification.Email);

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "UserLoggedOut",
                    new
                    {
                        notification.UserId,
                        notification.Email,
                        notification.LoggedOutAt
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (UserLoggedOut)");
            }
        }

        /// <summary>
        /// Gère l'événement de création de session
        /// </summary>
        public async Task Handle(SessionCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting SessionCreated: {Email} - Session {SessionId}",
                notification.Email,
                notification.SessionId);

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "SessionCreated",
                    new
                    {
                        notification.SessionId,
                        notification.UserId,
                        notification.Email,
                        notification.CreatedAt,
                        notification.ExpiresAt,
                        notification.IpAddress,
                        notification.UserAgent
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (SessionCreated)");
            }
        }

        /// <summary>
        /// Gère l'événement de création d'API Key
        /// </summary>
        public async Task Handle(ApiKeyCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting ApiKeyCreated: {Email} - Key '{KeyName}'",
                notification.Email,
                notification.KeyName);

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "ApiKeyCreated",
                    new
                    {
                        notification.ApiKeyId,
                        notification.UserId,
                        notification.Email,
                        notification.KeyName,
                        notification.CreatedAt,
                        notification.ExpiresAt
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (ApiKeyCreated)");
            }
        }

        /// <summary>
        /// Gère l'événement de révocation d'API Key
        /// </summary>
        public async Task Handle(ApiKeyRevokedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting ApiKeyRevoked: {Email} - Key '{KeyName}'",
                notification.Email,
                notification.KeyName);

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "ApiKeyRevoked",
                    new
                    {
                        notification.ApiKeyId,
                        notification.UserId,
                        notification.Email,
                        notification.KeyName,
                        notification.RevokedAt,
                        notification.RevokedBy
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (ApiKeyRevoked)");
            }
        }

        /// <summary>
        /// Gère l'événement de changement de rôle
        /// </summary>
        public async Task Handle(UserRoleChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting UserRoleChanged: {Email} - Role '{Role}' {Action}",
                notification.Email,
                notification.RoleName,
                notification.IsAdded ? "added" : "removed");

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "UserRoleChanged",
                    new
                    {
                        notification.UserId,
                        notification.Email,
                        notification.RoleName,
                        notification.IsAdded,
                        notification.ChangedAt,
                        notification.ChangedBy
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (UserRoleChanged)");
            }
        }

        /// <summary>
        /// Gère l'événement de changement de claim
        /// </summary>
        public async Task Handle(UserClaimChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🔐 [AdminHub] Broadcasting UserClaimChanged: {Email} - Claim '{ClaimType}={ClaimValue}' {Action}",
                notification.Email,
                notification.ClaimType,
                notification.ClaimValue,
                notification.IsAdded ? "added" : "removed");

            try
            {
                await _adminHubContext.Clients.All.SendAsync(
                    "UserClaimChanged",
                    new
                    {
                        notification.UserId,
                        notification.Email,
                        notification.ClaimType,
                        notification.ClaimValue,
                        notification.IsAdded,
                        notification.ChangedAt,
                        notification.ChangedBy
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (UserClaimChanged)");
            }
        }
    }
}

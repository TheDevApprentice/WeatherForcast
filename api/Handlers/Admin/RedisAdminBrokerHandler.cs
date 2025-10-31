using domain.Events;
using domain.Events.Admin;
using StackExchange.Redis;
using System.Text.Json;

namespace api.Handlers.Admin
{
    /// <summary>
    /// Publie les événements Admin sur Redis Pub/Sub pour propagation inter-process
    /// </summary>
    public class RedisAdminBrokerHandler :
        INotificationHandler<UserRegisteredEvent>,
        INotificationHandler<UserLoggedInEvent>,
        INotificationHandler<UserLoggedOutEvent>,
        INotificationHandler<SessionCreatedEvent>,
        INotificationHandler<ApiKeyCreatedEvent>,
        INotificationHandler<ApiKeyRevokedEvent>,
        INotificationHandler<UserRoleChangedEvent>,
        INotificationHandler<UserClaimChangedEvent>
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisAdminBrokerHandler> _logger;

        private const string ChUserRegistered = "admin.user.registered";
        private const string ChUserLoggedIn = "admin.user.loggedin";
        private const string ChUserLoggedOut = "admin.user.loggedout";
        private const string ChSessionCreated = "admin.session.created";
        private const string ChApiKeyCreated = "admin.apikey.created";
        private const string ChApiKeyRevoked = "admin.apikey.revoked";
        private const string ChUserRoleChanged = "admin.user.rolechanged";
        private const string ChUserClaimChanged = "admin.user.claimchanged";

        public RedisAdminBrokerHandler(IConnectionMultiplexer redis, ILogger<RedisAdminBrokerHandler> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private async Task PublishAsync(string channel, object payload)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("⚠️ API - [Redis Pub] Redis non connecté. Event non publié sur {Channel}", channel);
                    return;
                }

                var subscriber = _redis.GetSubscriber();
                var message = JsonSerializer.Serialize(payload);
                await subscriber.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal), message);
                _logger.LogInformation("📤 API - [Redis Pub] Event publié sur canal '{Channel}'", channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors de la publication Redis sur {Channel}", channel);
            }
        }

        public Task Handle(UserRegisteredEvent n, CancellationToken ct)
            => PublishAsync(ChUserRegistered, new
            {
                n.UserId,
                n.Email,
                n.UserName,
                n.RegisteredAt,
                n.IpAddress
            });

        public Task Handle(UserLoggedInEvent n, CancellationToken ct)
            => PublishAsync(ChUserLoggedIn, new
            {
                n.UserId,
                n.Email,
                n.UserName,
                n.LoggedInAt,
                n.IpAddress,
                n.UserAgent
            });

        public Task Handle(UserLoggedOutEvent n, CancellationToken ct)
            => PublishAsync(ChUserLoggedOut, new
            {
                n.UserId,
                n.Email,
                n.LoggedOutAt
            });

        public Task Handle(SessionCreatedEvent n, CancellationToken ct)
            => PublishAsync(ChSessionCreated, new
            {
                n.SessionId,
                n.UserId,
                n.Email,
                n.CreatedAt,
                n.ExpiresAt,
                n.IpAddress,
                n.UserAgent
            });

        public Task Handle(ApiKeyCreatedEvent n, CancellationToken ct)
            => PublishAsync(ChApiKeyCreated, new
            {
                n.ApiKeyId,
                n.UserId,
                n.Email,
                n.KeyName,
                n.CreatedAt,
                n.ExpiresAt
            });

        public Task Handle(ApiKeyRevokedEvent n, CancellationToken ct)
            => PublishAsync(ChApiKeyRevoked, new
            {
                n.ApiKeyId,
                n.UserId,
                n.Email,
                n.KeyName,
                n.RevokedAt,
                n.RevokedBy
            });

        public Task Handle(UserRoleChangedEvent n, CancellationToken ct)
            => PublishAsync(ChUserRoleChanged, new
            {
                n.UserId,
                n.Email,
                n.RoleName,
                n.IsAdded,
                n.ChangedAt,
                n.ChangedBy
            });

        public Task Handle(UserClaimChangedEvent n, CancellationToken ct)
            => PublishAsync(ChUserClaimChanged, new
            {
                n.UserId,
                n.Email,
                n.ClaimType,
                n.ClaimValue,
                n.IsAdded,
                n.ChangedAt,
                n.ChangedBy
            });
    }
}

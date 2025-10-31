using domain.Events;
using domain.Events.WeatherForecast;
using StackExchange.Redis;
using System.Text.Json;

namespace application.Handlers.WeatherForecast
{
    /// <summary>
    /// Handler qui publie les domain events sur Redis Pub/Sub
    /// Permet la communication inter-process entre l'Application Web et l'API
    /// </summary>
    public class RedisBrokerHandler :
        INotificationHandler<ForecastCreatedEvent>,
        INotificationHandler<ForecastUpdatedEvent>,
        INotificationHandler<ForecastDeletedEvent>
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisBrokerHandler> _logger;

        // Noms des canaux Redis
        private const string ChannelForecastCreated = "weatherforecast.created";
        private const string ChannelForecastUpdated = "weatherforecast.updated";
        private const string ChannelForecastDeleted = "weatherforecast.deleted";

        public RedisBrokerHandler(
            IConnectionMultiplexer redis,
            ILogger<RedisBrokerHandler> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// Publie l'event de création sur Redis
        /// </summary>
        public async Task Handle(ForecastCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Vérifier que Redis est connecté
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning(
                        "WEB - ⚠️ [Redis Pub] Redis non connecté. Event non publié - ID: {Id}",
                        notification.Forecast.Id);
                    return;
                }

                var subscriber = _redis.GetSubscriber();
                var message = JsonSerializer.Serialize(new
                {
                    SourceApp = "WEB",
                    Forecast = notification.Forecast
                });

                await subscriber.PublishAsync(
                    new RedisChannel(ChannelForecastCreated, RedisChannel.PatternMode.Literal),
                    message);

                _logger.LogInformation(
                    "📤 WEB - [Redis Pub] Event publié sur canal '{Channel}' - ID: {Id}",
                    ChannelForecastCreated,
                    notification.Forecast.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WEB - Erreur lors de la publication Redis (ForecastCreated)");
                // Ne pas throw pour ne pas bloquer les autres handlers
            }
        }

        /// <summary>
        /// Publie l'event de mise à jour sur Redis
        /// </summary>
        public async Task Handle(ForecastUpdatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Vérifier que Redis est connecté
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning(
                        "WEB - ⚠️ [Redis Pub] Redis non connecté. Event non publié - ID: {Id}",
                        notification.Forecast.Id);
                    return;
                }

                var subscriber = _redis.GetSubscriber();
                var message = JsonSerializer.Serialize(new
                {
                    SourceApp = "WEB",
                    Forecast = notification.Forecast
                });

                await subscriber.PublishAsync(
                    new RedisChannel(ChannelForecastUpdated, RedisChannel.PatternMode.Literal),
                    message);

                _logger.LogInformation(
                    "📤 WEB - [Redis Pub] Event publié sur canal '{Channel}' - ID: {Id}",
                    ChannelForecastUpdated,
                    notification.Forecast.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WEB - Erreur lors de la publication Redis (ForecastUpdated)");
            }
        }

        /// <summary>
        /// Publie l'event de suppression sur Redis
        /// </summary>
        public async Task Handle(ForecastDeletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Vérifier que Redis est connecté
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning(
                        "WEB - ⚠️ [Redis Pub] Redis non connecté. Event non publié - ID: {Id}",
                        notification.Id);
                    return;
                }

                var subscriber = _redis.GetSubscriber();

                // Pour la suppression, on envoie l'ID + la source
                var message = JsonSerializer.Serialize(new
                {
                    SourceApp = "WEB",
                    Id = notification.Id
                });

                await subscriber.PublishAsync(
                    new RedisChannel(ChannelForecastDeleted, RedisChannel.PatternMode.Literal),
                    message);

                _logger.LogInformation(
                    "📤 WEB - [Redis Pub] Event publié sur canal '{Channel}' - ID: {Id}",
                    ChannelForecastDeleted,
                    notification.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WEB - Erreur lors de la publication Redis (ForecastDeleted)");
            }
        }
    }
}

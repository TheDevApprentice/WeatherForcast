using MediatR;
using domain.Events.WeatherForecast;

namespace api.Handlers.WeatherForecast
{
    /// <summary>
    /// Handler qui log les events de prévisions météo dans l'API
    /// 
    /// NOTE: Pour que les notifications SignalR fonctionnent depuis l'API vers les clients Web,
    /// il faudrait :
    /// 1. Soit utiliser Redis Backplane pour SignalR (production)
    /// 2. Soit utiliser un message broker (RabbitMQ, Azure Service Bus, etc.)
    /// 3. Soit merger l'API et l'application Web dans le même process
    /// 
    /// Pour cette démo, ce handler log simplement les events.
    /// Les notifications temps réel fonctionnent uniquement depuis l'application Web.
    /// </summary>
    public class ApiAuditLogHandler :
        INotificationHandler<ForecastCreatedEvent>,
        INotificationHandler<ForecastUpdatedEvent>,
        INotificationHandler<ForecastDeletedEvent>
    {
        private readonly ILogger<ApiAuditLogHandler> _logger;

        public ApiAuditLogHandler(ILogger<ApiAuditLogHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ForecastCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📋 [API Audit] Forecast Created via API - ID: {Id}, Date: {Date}, Temp: {Temp}°C, By: {User}",
                notification.Forecast.Id,
                notification.Forecast.Date.ToString("yyyy-MM-dd"),
                notification.Forecast.TemperatureC,
                notification.TriggeredBy ?? "API Client");

            // TODO: Pour production, publier sur un message broker (Redis Pub/Sub, RabbitMQ)
            // await _messageBroker.PublishAsync("forecast.created", notification);

            return Task.CompletedTask;
        }

        public Task Handle(ForecastUpdatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📋 [API Audit] Forecast Updated via API - ID: {Id}, Date: {Date}, Temp: {Temp}°C",
                notification.Forecast.Id,
                notification.Forecast.Date.ToString("yyyy-MM-dd"),
                notification.Forecast.TemperatureC);

            return Task.CompletedTask;
        }

        public Task Handle(ForecastDeletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📋 [API Audit] Forecast Deleted via API - ID: {Id}",
                notification.Id);

            return Task.CompletedTask;
        }
    }
}

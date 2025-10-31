using domain.Events;
using domain.Events.WeatherForecast;

namespace application.Handlers.WeatherForecast
{
    /// <summary>
    /// Handler qui log les events de prévisions météo dans les logs
    /// Exemple d'extensibilité : on peut facilement ajouter un handler
    /// pour logger dans une base de données d'audit sans toucher au code existant
    /// </summary>
    public class AuditLogForecastHandler :
        INotificationHandler<ForecastCreatedEvent>,
        INotificationHandler<ForecastUpdatedEvent>,
        INotificationHandler<ForecastDeletedEvent>
    {
        private readonly ILogger<AuditLogForecastHandler> _logger;

        public AuditLogForecastHandler(ILogger<AuditLogForecastHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ForecastCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📋 WEB - [Audit] Forecast Created - ID: {Id}, Date: {Date}, Temp: {Temp}°C, By: {User}, At: {Timestamp}",
                notification.Forecast.Id,
                notification.Forecast.Date.ToString("yyyy-MM-dd"),
                notification.Forecast.TemperatureC,
                notification.TriggeredBy ?? "System",
                notification.Timestamp);

            // TODO: Ici on pourrait persister dans une table d'audit
            // await _auditRepository.CreateAsync(new AuditEntry { ... });

            return Task.CompletedTask;
        }

        public Task Handle(ForecastUpdatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📋 WEB - [Audit] Forecast Updated - ID: {Id}, Date: {Date}, Temp: {Temp}°C, By: {User}, At: {Timestamp}",
                notification.Forecast.Id,
                notification.Forecast.Date.ToString("yyyy-MM-dd"),
                notification.Forecast.TemperatureC,
                notification.TriggeredBy ?? "System",
                notification.Timestamp);

            return Task.CompletedTask;
        }

        public Task Handle(ForecastDeletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📋 WEB - [Audit] Forecast Deleted - ID: {Id}, By: {User}, At: {Timestamp}",
                notification.Id,
                notification.TriggeredBy ?? "System",
                notification.Timestamp);

            return Task.CompletedTask;
        }
    }
}

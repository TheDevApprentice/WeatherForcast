using MediatR;
using Microsoft.AspNetCore.SignalR;
using application.Hubs;
using domain.Events.WeatherForecast;

namespace application.Handlers.WeatherForecast
{
    /// <summary>
    /// Handler qui broadcaste les events de prévisions météo via SignalR
    /// Permet les mises à jour en temps réel sur tous les clients connectés
    /// </summary>
    public class SignalRForecastNotificationHandler :
        INotificationHandler<ForecastCreatedEvent>,
        INotificationHandler<ForecastUpdatedEvent>,
        INotificationHandler<ForecastDeletedEvent>
    {
        private readonly IHubContext<WeatherForecastHub> _hubContext;
        private readonly ILogger<SignalRForecastNotificationHandler> _logger;

        public SignalRForecastNotificationHandler(
            IHubContext<WeatherForecastHub> hubContext,
            ILogger<SignalRForecastNotificationHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Gère l'event de création de prévision
        /// </summary>
        public async Task Handle(ForecastCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📢 [SignalR] Broadcasting ForecastCreated: ID={Id}, TriggeredBy={User}",
                notification.Forecast.Id,
                notification.TriggeredBy ?? "System");

            try
            {
                await _hubContext.Clients.All
                    .SendAsync("ForecastCreated", notification.Forecast, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (ForecastCreated)");
                // Ne pas throw pour ne pas bloquer les autres handlers
            }
        }

        /// <summary>
        /// Gère l'event de mise à jour de prévision
        /// </summary>
        public async Task Handle(ForecastUpdatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📢 [SignalR] Broadcasting ForecastUpdated: ID={Id}",
                notification.Forecast.Id);

            try
            {
                await _hubContext.Clients.All
                    .SendAsync("ForecastUpdated", notification.Forecast, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (ForecastUpdated)");
            }
        }

        /// <summary>
        /// Gère l'event de suppression de prévision
        /// </summary>
        public async Task Handle(ForecastDeletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📢 [SignalR] Broadcasting ForecastDeleted: ID={Id}",
                notification.Id);

            try
            {
                await _hubContext.Clients.All
                    .SendAsync("ForecastDeleted", notification.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du broadcast SignalR (ForecastDeleted)");
            }
        }
    }
}

using domain.Events;
using domain.Events.WeatherForecast;
using Microsoft.AspNetCore.SignalR;
using shared.Hubs;

namespace api.Handlers.WeatherForecast
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
                "📢 API - [SignalR] Broadcasting ForecastCreated: ID={Id}, TriggeredBy={User}, ExcludedConnectionId={ConnectionId}",
                notification.Forecast.Id,
                notification.TriggeredBy ?? "System",
                notification.ExcludedConnectionId ?? "None");

            try
            {
                // Si un ConnectionId est fourni, exclure l'émetteur du broadcast
                var clients = string.IsNullOrEmpty(notification.ExcludedConnectionId)
                    ? _hubContext.Clients.All
                    : _hubContext.Clients.AllExcept(notification.ExcludedConnectionId);

                await clients.SendAsync("ForecastCreated", notification.Forecast, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors du broadcast SignalR (ForecastCreated)");
                // Ne pas throw pour ne pas bloquer les autres handlers
            }
        }

        /// <summary>
        /// Gère l'event de mise à jour de prévision
        /// </summary>
        public async Task Handle(ForecastUpdatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📢 API - [SignalR] Broadcasting ForecastUpdated: ID={Id}, ExcludedConnectionId={ConnectionId}",
                notification.Forecast.Id,
                notification.ExcludedConnectionId ?? "None");

            try
            {
                // Si un ConnectionId est fourni, exclure l'émetteur du broadcast
                var clients = string.IsNullOrEmpty(notification.ExcludedConnectionId)
                    ? _hubContext.Clients.All
                    : _hubContext.Clients.AllExcept(notification.ExcludedConnectionId);

                await clients.SendAsync("ForecastUpdated", notification.Forecast, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors du broadcast SignalR (ForecastUpdated)");
            }
        }

        /// <summary>
        /// Gère l'event de suppression de prévision
        /// </summary>
        public async Task Handle(ForecastDeletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📢 API - [SignalR] Broadcasting ForecastDeleted: ID={Id}, ExcludedConnectionId={ConnectionId}",
                notification.Id,
                notification.ExcludedConnectionId ?? "None");

            try
            {
                // Si un ConnectionId est fourni, exclure l'émetteur du broadcast
                var clients = string.IsNullOrEmpty(notification.ExcludedConnectionId)
                    ? _hubContext.Clients.All
                    : _hubContext.Clients.AllExcept(notification.ExcludedConnectionId);

                await clients.SendAsync("ForecastDeleted", notification.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors du broadcast SignalR (ForecastDeleted)");
            }
        }
    }
}

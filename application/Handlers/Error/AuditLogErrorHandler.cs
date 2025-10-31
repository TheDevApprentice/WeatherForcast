using domain.Events;
using domain.Events.Error;

namespace application.Handlers.Error
{
    /// <summary>
    /// Handler qui enregistre les erreurs dans les logs pour audit et debugging
    /// </summary>
    public class AuditLogErrorHandler : INotificationHandler<ErrorOccurredEvent>
    {
        private readonly ILogger<AuditLogErrorHandler> _logger;

        public AuditLogErrorHandler(ILogger<AuditLogErrorHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ErrorOccurredEvent notification, CancellationToken cancellationToken)
        {
            // Log structuré pour faciliter l'analyse et le monitoring
            if (notification.Exception != null)
            {
                _logger.LogError(
                    notification.Exception,
                    "WEB - [AUDIT ERROR] UserId={UserId} | Action={Action} | ErrorType={ErrorType} | Entity={EntityType}:{EntityId} | Message={Message} | CorrelationId={CorrelationId}",
                    notification.UserId,
                    notification.Action,
                    notification.ErrorType,
                    notification.EntityType ?? "N/A",
                    notification.EntityId ?? "N/A",
                    notification.ErrorMessage,
                    notification.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "WEB - [AUDIT ERROR] UserId={UserId} | Action={Action} | ErrorType={ErrorType} | Entity={EntityType}:{EntityId} | Message={Message} | CorrelationId={CorrelationId}",
                    notification.UserId,
                    notification.Action,
                    notification.ErrorType,
                    notification.EntityType ?? "N/A",
                    notification.EntityId ?? "N/A",
                    notification.ErrorMessage,
                    notification.CorrelationId);
            }

            return Task.CompletedTask;
        }
    }
}

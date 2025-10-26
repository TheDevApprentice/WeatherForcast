using domain.Events;
using domain.Events.Error;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using shared.Hubs;
using System.Text.Json;

namespace application.Handlers.Error
{
    /// <summary>
    /// Handler qui notifie l'utilisateur via SignalR lors d'une erreur
    /// Bufferise la notification dans Redis pour rattrapage après redirect/reload
    /// </summary>
    public class SignalRErrorHandler : INotificationHandler<ErrorOccurredEvent>
    {
        private readonly IHubContext<UsersHub> _usersHub;
        private readonly IPendingNotificationService _pending;
        private readonly ILogger<SignalRErrorHandler> _logger;

        public SignalRErrorHandler(
            IHubContext<UsersHub> usersHub,
            IPendingNotificationService pending,
            ILogger<SignalRErrorHandler> logger)
        {
            _usersHub = usersHub;
            _pending = pending;
            _logger = logger;
        }

        public async Task Handle(ErrorOccurredEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var payload = new
                {
                    Message = notification.ErrorMessage,
                    ErrorType = notification.ErrorType.ToString(),
                    Action = notification.Action,
                    EntityType = notification.EntityType,
                    EntityId = notification.EntityId,
                    OccurredAt = notification.OccurredAt,
                    CorrelationId = notification.CorrelationId
                };

                // Envoyer la notification SignalR à l'utilisateur concerné
                await _usersHub.Clients.User(notification.UserId).SendAsync(
                    "ErrorOccurred",
                    payload,
                    cancellationToken);

                _logger.LogInformation(
                    "[SignalR] Notification d'erreur envoyée à l'utilisateur {UserId} | Action={Action} | Type={ErrorType} | CorrelationId={CorrelationId}",
                    notification.UserId,
                    notification.Action,
                    notification.ErrorType,
                    notification.CorrelationId);

                // ✅ Bufferiser dans Redis UNIQUEMENT pour les erreurs qui causent un redirect
                // Les erreurs de validation (Validation) ne sont PAS bufferisées car l'utilisateur reste sur la page
                if (notification.ErrorType != domain.ValueObjects.ErrorType.Validation)
                {
                    // Bufferiser dans Redis pour rattrapage après redirect/reload
                    // TTL de 2 minutes pour laisser le temps au client de se reconnecter
                    var payloadJson = JsonSerializer.Serialize(payload);
                    await _pending.AddAsync(
                        "error",
                        notification.UserId,
                        "ErrorOccurred",
                        payloadJson,
                        TimeSpan.FromMinutes(2),
                        cancellationToken);

                    _logger.LogDebug(
                        "[Redis] Notification d'erreur bufferisée pour {UserId} | CorrelationId={CorrelationId}",
                        notification.UserId,
                        notification.CorrelationId);
                }
                else
                {
                    _logger.LogDebug(
                        "[Redis] Notification de validation NON bufferisée (user reste sur la page) | CorrelationId={CorrelationId}",
                        notification.CorrelationId);
                }
            }
            catch (Exception ex)
            {
                // Ne pas propager l'exception pour éviter de casser le flux principal
                _logger.LogError(
                    ex,
                    "Erreur lors de la notification SignalR d'erreur pour {UserId} | Action={Action} | CorrelationId={CorrelationId}",
                    notification.UserId,
                    notification.Action,
                    notification.CorrelationId);
            }
        }
    }
}

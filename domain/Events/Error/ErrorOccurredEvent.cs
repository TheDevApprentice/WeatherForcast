using domain.ValueObjects;

namespace domain.Events.Error
{
    /// <summary>
    /// Événement déclenché quand une erreur survient lors d'une action utilisateur
    /// Permet de notifier l'utilisateur en temps réel via SignalR
    /// </summary>
    public class ErrorOccurredEvent : INotification
    {
        /// <summary>
        /// ID de l'utilisateur concerné par l'erreur
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// Message d'erreur à afficher à l'utilisateur
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Type d'erreur (Validation, Database, External, Unknown)
        /// </summary>
        public ErrorType ErrorType { get; }

        /// <summary>
        /// Action qui a provoqué l'erreur (Create, Update, Delete, etc.)
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Entité concernée (WeatherForecast, ApiKey, User, etc.)
        /// </summary>
        public string? EntityType { get; }

        /// <summary>
        /// ID de l'entité concernée (si applicable)
        /// </summary>
        public string? EntityId { get; }

        /// <summary>
        /// Exception complète (pour logging uniquement, ne sera pas envoyée au client)
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Date et heure de l'erreur
        /// </summary>
        public DateTime OccurredAt { get; }

        /// <summary>
        /// Corrélation ID pour tracer l'erreur
        /// </summary>
        public string CorrelationId { get; }

        public ErrorOccurredEvent(
            string userId,
            string errorMessage,
            ErrorType errorType,
            string action,
            string? entityType = null,
            string? entityId = null,
            Exception? exception = null)
        {
            UserId = userId;
            ErrorMessage = errorMessage;
            ErrorType = errorType;
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            Exception = exception;
            OccurredAt = DateTime.UtcNow;
            CorrelationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        }
    }
}

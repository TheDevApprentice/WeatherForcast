using domain.ValueObjects;

namespace domain.Exceptions
{
    /// <summary>
    /// Exception de base pour toutes les exceptions métier du domain
    /// Contient les informations nécessaires pour créer un ErrorOccurredEvent
    /// </summary>
    public abstract class DomainException : Exception
    {
        /// <summary>
        /// Type d'erreur
        /// </summary>
        public abstract ErrorType ErrorType { get; }

        /// <summary>
        /// Action qui a provoqué l'erreur
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Type d'entité concernée
        /// </summary>
        public string? EntityType { get; }

        /// <summary>
        /// ID de l'entité concernée
        /// </summary>
        public string? EntityId { get; }

        protected DomainException(
            string message,
            string action,
            string? entityType = null,
            string? entityId = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
        }
    }
}

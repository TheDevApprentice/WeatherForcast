using domain.ValueObjects;

namespace domain.Exceptions
{
    /// <summary>
    /// Exception levée quand une entité n'est pas trouvée
    /// </summary>
    public class EntityNotFoundException : DomainException
    {
        public override ErrorType ErrorType => ErrorType.NotFound;

        public EntityNotFoundException(
            string message,
            string action,
            string? entityType = null,
            string? entityId = null)
            : base(message, action, entityType, entityId)
        {
        }

        public EntityNotFoundException(
            string entityType,
            string entityId,
            string action)
            : base($"{entityType} avec l'ID {entityId} introuvable.", action, entityType, entityId)
        {
        }
    }
}

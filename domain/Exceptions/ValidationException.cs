using domain.ValueObjects;

namespace domain.Exceptions
{
    /// <summary>
    /// Exception levée lors d'une erreur de validation
    /// </summary>
    public class ValidationException : DomainException
    {
        public override ErrorType ErrorType => ErrorType.Validation;

        public ValidationException(
            string message,
            string action,
            string? entityType = null,
            string? entityId = null)
            : base(message, action, entityType, entityId)
        {
        }
    }
}

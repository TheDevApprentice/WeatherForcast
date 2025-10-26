using domain.ValueObjects;

namespace domain.Exceptions
{
    /// <summary>
    /// Exception levée lors d'une erreur de base de données
    /// </summary>
    public class DatabaseException : DomainException
    {
        public override ErrorType ErrorType => ErrorType.Database;

        public DatabaseException(
            string message,
            string action,
            string? entityType = null,
            string? entityId = null,
            Exception? innerException = null)
            : base(message, action, entityType, entityId, innerException)
        {
        }
    }
}

using domain.Events;
using domain.Events.Error;
using domain.Exceptions;
using domain.ValueObjects;
using System.Security.Claims;

namespace application.Helpers
{
    /// <summary>
    /// Helper pour simplifier la publication d'événements d'erreur
    /// </summary>
    public static class ErrorHelper
    {
        /// <summary>
        /// Publie une erreur à partir d'une DomainException
        /// </summary>
        public static async Task PublishDomainExceptionAsync(
            this IPublisher publisher,
            ClaimsPrincipal user,
            DomainException domainException,
            CancellationToken cancellationToken = default)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            // ✅ Plus besoin de mapping, ErrorType est déjà un enum
            var errorEvent = new ErrorOccurredEvent(
                userId,
                domainException.Message,
                domainException.ErrorType,
                domainException.Action,
                domainException.EntityType,
                domainException.EntityId,
                domainException);

            await publisher.Publish(errorEvent, cancellationToken);
        }
        /// <summary>
        /// Publie une erreur pour l'utilisateur connecté
        /// </summary>
        public static async Task PublishErrorAsync(
            this IPublisher publisher,
            ClaimsPrincipal user,
            string errorMessage,
            ErrorType errorType,
            string action,
            string? entityType = null,
            string? entityId = null,
            Exception? exception = null,
            CancellationToken cancellationToken = default)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Si l'utilisateur n'est pas authentifié, on ne peut pas publier l'erreur
                return;
            }

            var errorEvent = new ErrorOccurredEvent(
                userId,
                errorMessage,
                errorType,
                action,
                entityType,
                entityId,
                exception);

            await publisher.Publish(errorEvent, cancellationToken);
        }

        /// <summary>
        /// Publie une erreur de validation
        /// </summary>
        public static Task PublishValidationErrorAsync(
            this IPublisher publisher,
            ClaimsPrincipal user,
            string errorMessage,
            string action,
            string? entityType = null,
            string? entityId = null,
            Exception? exception = null,
            CancellationToken cancellationToken = default)
        {
            return publisher.PublishErrorAsync(
                user,
                errorMessage,
                ErrorType.Validation,
                action,
                entityType,
                entityId,
                exception,
                cancellationToken);
        }

        /// <summary>
        /// Publie une erreur de base de données
        /// </summary>
        public static Task PublishDatabaseErrorAsync(
            this IPublisher publisher,
            ClaimsPrincipal user,
            string errorMessage,
            string action,
            string? entityType = null,
            string? entityId = null,
            Exception? exception = null,
            CancellationToken cancellationToken = default)
        {
            return publisher.PublishErrorAsync(
                user,
                errorMessage,
                ErrorType.Database,
                action,
                entityType,
                entityId,
                exception,
                cancellationToken);
        }

        /// <summary>
        /// Publie une erreur "entité non trouvée"
        /// </summary>
        public static Task PublishNotFoundErrorAsync(
            this IPublisher publisher,
            ClaimsPrincipal user,
            string errorMessage,
            string action,
            string? entityType = null,
            string? entityId = null,
            CancellationToken cancellationToken = default)
        {
            return publisher.PublishErrorAsync(
                user,
                errorMessage,
                ErrorType.NotFound,
                action,
                entityType,
                entityId,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Publie une erreur générique
        /// </summary>
        public static Task PublishGenericErrorAsync(
            this IPublisher publisher,
            ClaimsPrincipal user,
            string errorMessage,
            string action,
            string? entityType = null,
            string? entityId = null,
            Exception? exception = null,
            CancellationToken cancellationToken = default)
        {
            return publisher.PublishErrorAsync(
                user,
                errorMessage,
                ErrorType.Unknown,
                action,
                entityType,
                entityId,
                exception,
                cancellationToken);
        }
    }
}

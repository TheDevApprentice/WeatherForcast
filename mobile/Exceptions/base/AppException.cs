namespace mobile.Exceptions
{
    /// <summary>
    /// Exception de base pour toutes les exceptions métier de l'application
    /// </summary>
    public abstract class AppException : Exception
    {
        public string UserMessage { get; }
        public string? ErrorCode { get; }
        public Dictionary<string, object>? AdditionalData { get; }

        protected AppException(
            string userMessage,
            string? errorCode = null,
            string? technicalMessage = null,
            Exception? innerException = null,
            Dictionary<string, object>? additionalData = null)
            : base(technicalMessage ?? userMessage, innerException)
        {
            UserMessage = userMessage;
            ErrorCode = errorCode;
            AdditionalData = additionalData;
        }
    }

    /// <summary>
    /// Exception réseau (pas de connexion, timeout, etc.)
    /// </summary>
    public class NetworkException : AppException
    {
        public NetworkException(
            string? technicalMessage = null,
            Exception? innerException = null)
            : base(
                "Impossible de se connecter au serveur. Vérifiez votre connexion Internet.",
                "NETWORK_ERROR",
                technicalMessage,
                innerException)
        {
        }
    }

    /// <summary>
    /// Exception d'authentification (token invalide, expiré, etc.)
    /// </summary>
    public class AuthenticationException : AppException
    {
        public AuthenticationException(
            string? technicalMessage = null,
            Exception? innerException = null)
            : base(
                "Votre session a expiré. Veuillez vous reconnecter.",
                "AUTH_ERROR",
                technicalMessage,
                innerException)
        {
        }
    }

    /// <summary>
    /// Exception d'autorisation (permissions insuffisantes)
    /// </summary>
    public class AuthorizationException : AppException
    {
        public AuthorizationException(
            string? technicalMessage = null,
            Exception? innerException = null)
            : base(
                "Vous n'avez pas les permissions nécessaires pour cette action.",
                "AUTHORIZATION_ERROR",
                technicalMessage,
                innerException)
        {
        }
    }

    /// <summary>
    /// Exception de validation (données invalides)
    /// </summary>
    public class ValidationException : AppException
    {
        public ValidationException(
            string userMessage,
            Dictionary<string, object>? validationErrors = null,
            Exception? innerException = null)
            : base(
                userMessage,
                "VALIDATION_ERROR",
                null,
                innerException,
                validationErrors)
        {
        }
    }

    /// <summary>
    /// Exception serveur (erreur 500, etc.)
    /// </summary>
    public class ServerException : AppException
    {
        public int StatusCode { get; }

        public ServerException(
            int statusCode,
            string? technicalMessage = null,
            Exception? innerException = null)
            : base(
                "Une erreur est survenue sur le serveur. Veuillez réessayer plus tard.",
                "SERVER_ERROR",
                technicalMessage,
                innerException)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Exception de ressource non trouvée (404)
    /// </summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(
            string resourceName,
            Exception? innerException = null)
            : base(
                $"La ressource '{resourceName}' est introuvable.",
                "NOT_FOUND",
                null,
                innerException)
        {
        }
    }

    /// <summary>
    /// Exception de conflit (409, ressource déjà existante)
    /// </summary>
    public class ConflictException : AppException
    {
        public ConflictException(
            string userMessage,
            Exception? innerException = null)
            : base(
                userMessage,
                "CONFLICT",
                null,
                innerException)
        {
        }
    }

    /// <summary>
    /// Exception de limite de taux (429, trop de requêtes)
    /// </summary>
    public class RateLimitException : AppException
    {
        public TimeSpan? RetryAfter { get; }

        public RateLimitException(
            TimeSpan? retryAfter = null,
            Exception? innerException = null)
            : base(
                "Trop de requêtes. Veuillez patienter quelques instants.",
                "RATE_LIMIT",
                null,
                innerException)
        {
            RetryAfter = retryAfter;
        }
    }
}

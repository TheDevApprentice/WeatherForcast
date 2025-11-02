using Microsoft.Extensions.Logging;
using mobile.Services.Exceptions;
using System.Net;

namespace mobile.Services.Middleware
{
    /// <summary>
    /// Middleware de gestion centralisée des erreurs HTTP
    /// Transforme les erreurs HTTP en exceptions typées
    /// </summary>
    public class ErrorHandlingMiddleware : DelegatingHandler
    {
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
        {
            _logger = logger;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
#if DEBUG
                _logger.LogDebug("HTTP Request: {Method} {Url}", request.Method, request.RequestUri);
#endif

                var response = await base.SendAsync(request, cancellationToken);

                // Si la réponse est OK, retourner directement
                if (response.IsSuccessStatusCode)
                {
#if DEBUG
                    _logger.LogDebug("HTTP Response: {StatusCode} {Url}", response.StatusCode, request.RequestUri);
#endif
                    return response;
                }

                // Sinon, transformer en exception typée
                await HandleErrorResponseAsync(response, request);
                return response; // Ne sera jamais atteint car HandleErrorResponseAsync lance une exception
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur réseau lors de la requête HTTP: {Url}", request.RequestUri);
                throw new NetworkException($"Erreur réseau: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timeout lors de la requête HTTP: {Url}", request.RequestUri);
                throw new NetworkException("La requête a pris trop de temps. Veuillez réessayer.", ex);
            }
            catch (AppException)
            {
                // Laisser passer les exceptions métier
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la requête HTTP: {Url}", request.RequestUri);
                throw;
            }
        }

        /// <summary>
        /// Transforme une réponse HTTP en erreur en exception typée
        /// </summary>
        private async Task HandleErrorResponseAsync(HttpResponseMessage response, HttpRequestMessage request)
        {
            var statusCode = (int)response.StatusCode;
            var content = await response.Content.ReadAsStringAsync();

#if DEBUG
            _logger.LogWarning(
                "HTTP Error: {StatusCode} {Url} - Content: {Content}",
                statusCode,
                request.RequestUri,
                content);
#endif

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized: // 401
                    _logger.LogWarning("Authentification échouée: {Url}", request.RequestUri);
                    throw new AuthenticationException($"Authentification échouée (401): {content}");

                case HttpStatusCode.Forbidden: // 403
                    _logger.LogWarning("Autorisation refusée: {Url}", request.RequestUri);
                    throw new AuthorizationException($"Autorisation refusée (403): {content}");

                case HttpStatusCode.NotFound: // 404
                    _logger.LogWarning("Ressource non trouvée: {Url}", request.RequestUri);
                    throw new NotFoundException($"Ressource non trouvée: {request.RequestUri?.PathAndQuery}");

                case HttpStatusCode.Conflict: // 409
                    _logger.LogWarning("Conflit: {Url}", request.RequestUri);
                    throw new ConflictException($"Conflit: {content}");

                case HttpStatusCode.BadRequest: // 400
                    _logger.LogWarning("Requête invalide: {Url}", request.RequestUri);
                    throw new ValidationException($"Requête invalide: {content}");

                case (HttpStatusCode)429: // Too Many Requests
                    _logger.LogWarning("Limite de taux atteinte: {Url}", request.RequestUri);
                    
                    TimeSpan? retryAfter = null;
                    if (response.Headers.RetryAfter?.Delta != null)
                    {
                        retryAfter = response.Headers.RetryAfter.Delta;
                    }
                    
                    throw new RateLimitException(retryAfter);

                case HttpStatusCode.InternalServerError: // 500
                case HttpStatusCode.BadGateway: // 502
                case HttpStatusCode.ServiceUnavailable: // 503
                case HttpStatusCode.GatewayTimeout: // 504
                    _logger.LogError("Erreur serveur: {StatusCode} {Url}", statusCode, request.RequestUri);
                    throw new ServerException(statusCode, $"Erreur serveur ({statusCode}): {content}");

                default:
                    _logger.LogError("Erreur HTTP non gérée: {StatusCode} {Url}", statusCode, request.RequestUri);
                    throw new ServerException(statusCode, $"Erreur HTTP ({statusCode}): {content}");
            }
        }
    }
}

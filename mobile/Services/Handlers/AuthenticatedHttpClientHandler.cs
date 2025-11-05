using Microsoft.Extensions.Logging;
using mobile.Exceptions;
using System.Net;
using System.Net.Http.Headers;

namespace mobile.Services.Handlers
{
    /// <summary>
    /// Handler HTTP qui ajoute automatiquement le JWT Bearer Token aux requêtes
    /// Inclut un système de retry avec backoff exponentiel pour gérer le cas où l'API démarre après l'app mobile
    /// </summary>
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly INetworkMonitorService _networkMonitor;
        private readonly ILogger<AuthenticatedHttpClientHandler> _logger;
        private const int MaxRetries = 3;
        private const int BaseDelayMilliseconds = 1000;
        private static readonly Random _random = new();

        public AuthenticatedHttpClientHandler(
            ISecureStorageService secureStorage,
            INetworkMonitorService networkMonitor,
            ILogger<AuthenticatedHttpClientHandler> logger)
        {
            _secureStorage = secureStorage;
            _networkMonitor = networkMonitor;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // ✅ VÉRIFIER LE RÉSEAU AVANT TOUT APPEL HTTP
            if (!_networkMonitor.IsNetworkAvailable)
            {
#if DEBUG
                _logger.LogWarning("📡 Pas de réseau disponible - Annulation de la requête {Method} {Url}", 
                    request.Method, request.RequestUri);
#endif
                throw new NetworkUnavailableExecption(
                    "Vous êtes hors ligne. Veuillez vérifier votre connexion.",
                    "Network is not available"
                );
            }

            // Récupérer le token JWT
            var token = await _secureStorage.GetTokenAsync();

#if DEBUG
            _logger.LogInformation("Token récupéré: {Status} - Request: {Method} {Url}",
                string.IsNullOrEmpty(token) ? "VIDE" : "OK",
                request.Method,
                request.RequestUri);
#endif

            // Ajouter le header Authorization si le token existe
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Système de retry pour gérer le cas où l'API n'est pas encore démarrée
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var response = await base.SendAsync(request, cancellationToken);

                    // Si succès, retourner la réponse
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    // Gérer les erreurs réseau qui nécessitent un retry
                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable || 
                        response.StatusCode == HttpStatusCode.BadGateway ||
                        response.StatusCode == HttpStatusCode.GatewayTimeout)
                    {
                        // Si c'est la dernière tentative, lever ApiUnavailableException
                        if (attempt == MaxRetries)
                        {
                            _logger.LogWarning("API indisponible après {Attempts} tentatives - Code: {StatusCode}", 
                                MaxRetries, response.StatusCode);
                            throw new ApiUnavailableException(
                                $"API inaccessible après {MaxRetries} tentatives - Code {response.StatusCode}");
                        }

                        // Calculer le délai avec backoff exponentiel et jitter
                        var delay = CalculateBackoffDelay(attempt);
                        _logger.LogWarning("API indisponible (tentative {Attempt}/{Max}), nouvelle tentative dans {Delay}ms...",
                            attempt, MaxRetries, delay);
                        
                        await Task.Delay(delay, cancellationToken);
                        continue; // Réessayer
                    }

                    // Pour les autres erreurs (4xx, 5xx non-réseau), gérer via HandleErrorResponseAsync
                    await HandleErrorResponseAsync(response, request);
                    
                    // Si HandleErrorResponseAsync ne lève pas d'exception, retourner la réponse
                    return response;
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    // Erreur réseau : l'API n'est probablement pas encore démarrée
                    var delay = CalculateBackoffDelay(attempt);
                    _logger.LogWarning(ex, "Erreur réseau (tentative {Attempt}/{Max}), nouvelle tentative dans {Delay}ms...",
                        attempt, MaxRetries, delay);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (HttpRequestException ex) when (attempt == MaxRetries)
                {
                    // Dernière tentative échouée - Lever ApiUnavailableException
                    _logger.LogError(ex, "Erreur réseau après {Attempts} tentatives", MaxRetries);
                    throw new ApiUnavailableException("API non joignable - Erreur réseau", ex);
                }
                catch (TaskCanceledException ex) when (attempt < MaxRetries)
                {
                    // Timeout - Réessayer
                    var delay = CalculateBackoffDelay(attempt);
                    _logger.LogWarning(ex, "Timeout (tentative {Attempt}/{Max}), nouvelle tentative dans {Delay}ms...",
                        attempt, MaxRetries, delay);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException ex) when (attempt == MaxRetries)
                {
                    // Timeout après toutes les tentatives
                    _logger.LogError(ex, "Timeout après {Attempts} tentatives", MaxRetries);
                    throw new ApiUnavailableException("API non joignable - Timeout", ex);
                }
            }

            // Ne devrait jamais arriver ici
            throw new ApiUnavailableException("Échec de la requête après plusieurs tentatives");
        }

        /// <summary>
        /// Calcule le délai d'attente avec backoff exponentiel et jitter
        /// Formule: BaseDelay * 2^(attempt-1) + jitter aléatoire
        /// </summary>
        /// <param name="attempt">Numéro de la tentative (1-based)</param>
        /// <returns>Délai en millisecondes</returns>
        private static int CalculateBackoffDelay(int attempt)
        {
            // Backoff exponentiel: 1s, 2s, 4s, 8s, etc.
            var exponentialDelay = BaseDelayMilliseconds * Math.Pow(2, attempt - 1);
            
            // Ajouter un jitter aléatoire de 0-100ms pour éviter le thundering herd
            var jitter = _random.Next(0, 100);
            
            // Limiter à un maximum de 10 secondes
            var totalDelay = Math.Min(exponentialDelay + jitter, 10000);
            
            return (int)totalDelay;
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
                // Erreurs d'authentification/autorisation (4xx)
                case HttpStatusCode.Unauthorized: // 401
                    _logger.LogWarning("Authentification échouée: {Url}", request.RequestUri);
                    // Ne pas lever d'exception pour 401 - laisser le code appelant gérer
                    // (ex: ValidateSessionAsync retourne false)
                    break;

                case HttpStatusCode.Forbidden: // 403
                    _logger.LogWarning("Autorisation refusée: {Url}", request.RequestUri);
                    throw new UnauthorizedAccessException($"Autorisation refusée (403): {content}");

                case HttpStatusCode.NotFound: // 404
                    _logger.LogWarning("Ressource non trouvée: {Url}", request.RequestUri);
                    throw new InvalidOperationException($"Ressource non trouvée: {request.RequestUri?.PathAndQuery}");

                case HttpStatusCode.Conflict: // 409
                    _logger.LogWarning("Conflit: {Url}", request.RequestUri);
                    throw new InvalidOperationException($"Conflit: {content}");

                case HttpStatusCode.BadRequest: // 400
                    _logger.LogWarning("Requête invalide: {Url}", request.RequestUri);
                    throw new ArgumentException($"Requête invalide: {content}");

                case (HttpStatusCode)422: // Unprocessable Entity
                    _logger.LogWarning("Validation échouée: {Url}", request.RequestUri);
                    throw new ArgumentException($"Validation échouée: {content}");

                case (HttpStatusCode)429: // Too Many Requests
                    _logger.LogWarning("Limite de taux atteinte: {Url}", request.RequestUri);
                    
                    TimeSpan? retryAfter = null;
                    if (response.Headers.RetryAfter?.Delta != null)
                    {
                        retryAfter = response.Headers.RetryAfter.Delta;
                    }
                    
                    throw new InvalidOperationException(
                        $"Limite de taux atteinte. Réessayez dans {retryAfter?.TotalSeconds ?? 60} secondes.");

                // Erreurs serveur (5xx) - Note: 502, 503, 504 sont déjà gérés dans le retry
                case HttpStatusCode.InternalServerError: // 500
                    _logger.LogError("Erreur serveur interne: {StatusCode} {Url}", statusCode, request.RequestUri);
                    throw new InvalidOperationException($"Erreur serveur interne (500): {content}");

                // Autres erreurs non gérées
                default:
                    if (statusCode >= 500)
                    {
                        _logger.LogError("Erreur serveur: {StatusCode} {Url}", statusCode, request.RequestUri);
                        throw new InvalidOperationException($"Erreur serveur ({statusCode}): {content}");
                    }
                    else if (statusCode >= 400)
                    {
                        _logger.LogWarning("Erreur client: {StatusCode} {Url}", statusCode, request.RequestUri);
                        throw new InvalidOperationException($"Erreur client ({statusCode}): {content}");
                    }
                    break;
            }
        }
    }
}

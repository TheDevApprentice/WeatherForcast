using mobile.Exceptions;
using mobile.Services.Internal.Interfaces;
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
        private const int MaxRetries = 3;
        private const int BaseDelayMilliseconds = 1000;
        private static readonly Random _random = new();

        public AuthenticatedHttpClientHandler (
            ISecureStorageService secureStorage,
            INetworkMonitorService networkMonitor)
        {
            _secureStorage = secureStorage;
            _networkMonitor = networkMonitor;
        }

        protected override async Task<HttpResponseMessage> SendAsync (
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // ✅ VÉRIFIER LE RÉSEAU AVANT TOUT APPEL HTTP
            if (!_networkMonitor.IsNetworkAvailable)
            {
                // Pas de réseau disponible - Annulation de la requête
                throw new NetworkUnavailableExecption(
                    "Vous êtes hors ligne. Veuillez vérifier votre connexion.",
                    "Network is not available"
                );
            }

            // Récupérer le token JWT
            var token = await _secureStorage.GetTokenAsync();

            // Token récupéré pour authentification

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
                            // API indisponible après toutes les tentatives
                            throw new ApiUnavailableException(
                                $"API inaccessible après {MaxRetries} tentatives - Code {response.StatusCode}");
                        }

                        // Calculer le délai avec backoff exponentiel et jitter
                        var delay = CalculateBackoffDelay(attempt);
                        // API indisponible, nouvelle tentative avec backoff

                        await Task.Delay(delay, cancellationToken);
                        continue; // Réessayer
                    }

                    // Pour les autres erreurs (4xx, 5xx non-réseau), gérer via HandleErrorResponseAsync
                    await HandleErrorResponseAsync(response, request);

                    // Si HandleErrorResponseAsync ne lève pas d'exception, retourner la réponse
                    return response;
                }
                catch (ApiUnavailableException ex)
                {
                    throw new ApiUnavailableException(ex.Message, ex);
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    // Erreur réseau : l'API n'est probablement pas encore démarrée
                    var delay = CalculateBackoffDelay(attempt);
                    // Erreur réseau, nouvelle tentative avec backoff
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException ex) when (attempt < MaxRetries)
                {
                    // Timeout - Réessayer
                    var delay = CalculateBackoffDelay(attempt);
                    // Timeout, nouvelle tentative avec backoff
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException ex) when (attempt == MaxRetries)
                {
                    // Timeout après toutes les tentatives
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
        private static int CalculateBackoffDelay (int attempt)
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
        private async Task HandleErrorResponseAsync (HttpResponseMessage response, HttpRequestMessage request)
        {
            var statusCode = (int)response.StatusCode;
            var content = await response.Content.ReadAsStringAsync();

            // Erreur HTTP détectée

            switch (response.StatusCode)
            {
                // Erreurs d'authentification/autorisation (4xx)
                case HttpStatusCode.Unauthorized: // 401
                    // Authentification échouée
                    // Ne pas lever d'exception pour 401 - laisser le code appelant gérer
                    // (ex: ValidateSessionAsync retourne false)
                    break;

                case HttpStatusCode.Forbidden: // 403
                    // Autorisation refusée
                    throw new UnauthorizedAccessException($"Autorisation refusée (403): {content}");

                case HttpStatusCode.NotFound: // 404
                    // Ressource non trouvée
                    throw new InvalidOperationException($"Ressource non trouvée: {request.RequestUri?.PathAndQuery}");

                case HttpStatusCode.Conflict: // 409
                    // Conflit
                    throw new InvalidOperationException($"Conflit: {content}");

                case HttpStatusCode.BadRequest: // 400
                    // Requête invalide
                    throw new ArgumentException($"Requête invalide: {content}");

                case (HttpStatusCode)422: // Unprocessable Entity
                    // Validation échouée
                    throw new ArgumentException($"Validation échouée: {content}");

                case (HttpStatusCode)429: // Too Many Requests
                    // Limite de taux atteinte

                    TimeSpan? retryAfter = null;
                    if (response.Headers.RetryAfter?.Delta != null)
                    {
                        retryAfter = response.Headers.RetryAfter.Delta;
                    }

                    throw new InvalidOperationException(
                        $"Limite de taux atteinte. Réessayez dans {retryAfter?.TotalSeconds ?? 60} secondes.");

                // Erreurs serveur (5xx) - Note: 502, 503, 504 sont déjà gérés dans le retry
                case HttpStatusCode.InternalServerError: // 500
                    // Erreur serveur interne
                    throw new InvalidOperationException($"Erreur serveur interne (500): {content}");

                // Autres erreurs non gérées
                default:
                    if (statusCode >= 500)
                    {
                        // Erreur serveur
                        throw new InvalidOperationException($"Erreur serveur ({statusCode}): {content}");
                    }
                    else if (statusCode >= 400)
                    {
                        // Erreur client
                        throw new InvalidOperationException($"Erreur client ({statusCode}): {content}");
                    }
                    break;
            }
        }
    }
}

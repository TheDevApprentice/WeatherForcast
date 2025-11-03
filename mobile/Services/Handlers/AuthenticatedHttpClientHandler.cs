using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuthenticatedHttpClientHandler> _logger;
        private const int MaxRetries = 3;
        private const int BaseDelayMilliseconds = 1000;
        private static readonly Random _random = new();

        public AuthenticatedHttpClientHandler(
            ISecureStorageService secureStorage,
            ILogger<AuthenticatedHttpClientHandler> logger)
        {
            _secureStorage = secureStorage;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Récupérer le token JWT
            var token = await _secureStorage.GetTokenAsync();

            _logger.LogInformation("Token récupéré: {Status} - Request: {Method} {Url}",
                string.IsNullOrEmpty(token) ? "VIDE" : "OK",
                request.Method,
                request.RequestUri);

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

                    // Si succès ou erreur non-réseau, retourner la réponse
                    if (response.IsSuccessStatusCode || 
                        (response.StatusCode != HttpStatusCode.ServiceUnavailable && 
                         response.StatusCode != HttpStatusCode.BadGateway))
                    {
                        return response;
                    }

                    // Si c'est la dernière tentative, retourner l'erreur
                    if (attempt == MaxRetries)
                    {
                        _logger.LogWarning("API indisponible après {Attempts} tentatives", MaxRetries);
                        return response;
                    }

                    // Calculer le délai avec backoff exponentiel et jitter
                    var delay = CalculateBackoffDelay(attempt);
                    _logger.LogWarning("API indisponible (tentative {Attempt}/{Max}), nouvelle tentative dans {Delay}ms...",
                        attempt, MaxRetries, delay);
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    // Erreur réseau : l'API n'est probablement pas encore démarrée
                    var delay = CalculateBackoffDelay(attempt);
                    _logger.LogWarning(ex, "Erreur réseau (tentative {Attempt}/{Max}), nouvelle tentative dans {Delay}ms...",
                        attempt, MaxRetries, delay);
                }
                catch (HttpRequestException ex) when (attempt == MaxRetries)
                {
                    // Dernière tentative échouée
                    _logger.LogError(ex, "Erreur réseau après {Attempts} tentatives", MaxRetries);
                    throw;
                }

                // Attendre avant de réessayer avec backoff exponentiel
                var delayMs = CalculateBackoffDelay(attempt);
                await Task.Delay(delayMs, cancellationToken);
            }

            // Ne devrait jamais arriver ici
            throw new HttpRequestException("Échec de la requête après plusieurs tentatives");
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
    }
}

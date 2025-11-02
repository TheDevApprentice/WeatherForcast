using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;

namespace mobile.Services.Handlers
{
    /// <summary>
    /// Handler HTTP qui ajoute automatiquement le JWT Bearer Token aux requêtes
    /// Inclut un système de retry pour gérer le cas où l'API démarre après l'app mobile
    /// </summary>
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly ILogger<AuthenticatedHttpClientHandler> _logger;
        private const int MaxRetries = 3;
        private const int DelayMilliseconds = 1000;

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

                    _logger.LogWarning("API indisponible (tentative {Attempt}/{Max}), nouvelle tentative dans {Delay}ms...",
                        attempt, MaxRetries, DelayMilliseconds);
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    // Erreur réseau : l'API n'est probablement pas encore démarrée
                    _logger.LogWarning(ex, "Erreur réseau (tentative {Attempt}/{Max}), nouvelle tentative dans {Delay}ms...",
                        attempt, MaxRetries, DelayMilliseconds);
                }
                catch (HttpRequestException ex) when (attempt == MaxRetries)
                {
                    // Dernière tentative échouée
                    _logger.LogError(ex, "Erreur réseau après {Attempts} tentatives", MaxRetries);
                    throw;
                }

                // Attendre avant de réessayer
                await Task.Delay(DelayMilliseconds * attempt, cancellationToken);
            }

            // Ne devrait jamais arriver ici
            throw new HttpRequestException("Échec de la requête après plusieurs tentatives");
        }
    }
}

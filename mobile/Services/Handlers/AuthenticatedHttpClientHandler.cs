using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace mobile.Services.Handlers
{
    /// <summary>
    /// Handler HTTP qui ajoute automatiquement le JWT Bearer Token aux requêtes
    /// </summary>
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly ILogger<AuthenticatedHttpClientHandler> _logger;

        public AuthenticatedHttpClientHandler(
            ISecureStorageService secureStorage,
            ILogger<AuthenticatedHttpClientHandler> logger)
        {
            _secureStorage = secureStorage;
            _logger = logger;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Récupérer le token JWT
            var token = await _secureStorage.GetTokenAsync();

#if DEBUG
            _logger.LogDebug(
                "Token récupéré: {Status} - Request URL: {Url}",
                string.IsNullOrEmpty(token) ? "VIDE" : $"{token.Substring(0, Math.Min(20, token.Length))}...",
                request.RequestUri);
#endif

            // Ajouter le header Authorization si le token existe
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
#if DEBUG
                _logger.LogDebug("Authorization header ajouté pour {Url}", request.RequestUri);
#endif
            }
            else
            {
#if DEBUG
                _logger.LogWarning("ATTENTION: Token vide, pas d'Authorization header pour {Url}", request.RequestUri);
#endif
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

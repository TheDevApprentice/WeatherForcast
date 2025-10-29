using System.Net.Http.Headers;

namespace mobile.Services.Handlers
{
    /// <summary>
    /// Handler HTTP qui ajoute automatiquement le JWT Bearer Token aux requêtes
    /// </summary>
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _secureStorage;

        public AuthenticatedHttpClientHandler(ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Récupérer le token JWT
            var token = await _secureStorage.GetTokenAsync();

            // Ajouter le header Authorization si le token existe
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

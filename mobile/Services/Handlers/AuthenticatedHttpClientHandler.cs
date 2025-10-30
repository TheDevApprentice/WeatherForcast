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

            Console.WriteLine($"[AuthHandler] Token récupéré: {(string.IsNullOrEmpty(token) ? "VIDE" : $"{token.Substring(0, Math.Min(20, token.Length))}...")}");
            Console.WriteLine($"[AuthHandler] Request URL: {request.RequestUri}");

            // Ajouter le header Authorization si le token existe
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"[AuthHandler] Authorization header ajouté");
            }
            else
            {
                Console.WriteLine($"[AuthHandler] ATTENTION: Token vide, pas d'Authorization header");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

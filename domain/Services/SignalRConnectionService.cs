using Microsoft.AspNetCore.Http;

namespace domain.Services
{
    /// <summary>
    /// Service pour récupérer le ConnectionId SignalR de l'utilisateur actuel
    /// Utilisé pour exclure l'émetteur des notifications SignalR
    /// </summary>
    public interface ISignalRConnectionService
    {
        string? GetCurrentConnectionId();
    }

    public class SignalRConnectionService : ISignalRConnectionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SignalRConnectionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Récupère le ConnectionId SignalR stocké dans les items de la requête HTTP
        /// Retourne null si aucun ConnectionId n'est trouvé (ex: appel depuis l'API)
        /// </summary>
        public string? GetCurrentConnectionId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Le ConnectionId est stocké par le client JavaScript dans un cookie ou header
            // Pour simplifier, on va le stocker dans un cookie nommé "SignalR-ConnectionId"
            if (httpContext.Request.Cookies.TryGetValue("SignalR-ConnectionId", out var connectionId))
            {
                return connectionId;
            }

            return null;
        }
    }
}

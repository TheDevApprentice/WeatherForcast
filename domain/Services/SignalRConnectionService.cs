using Microsoft.AspNetCore.Http;
using shared.Services;
using System.Security.Claims;

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
        private readonly IConnectionMappingService _connectionMapping;

        public SignalRConnectionService(
            IHttpContextAccessor httpContextAccessor,
            IConnectionMappingService connectionMapping)
        {
            _httpContextAccessor = httpContextAccessor;
            _connectionMapping = connectionMapping;
        }

        /// <summary>
        /// Récupère le ConnectionId SignalR de l'utilisateur actuel
        /// Méthode 1 (Web) : Cookie "SignalR-ConnectionId"
        /// Méthode 2 (Mobile/API) : Mapping Redis userId → connectionId
        /// </summary>
        public string? GetCurrentConnectionId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Méthode 1 : Cookie (Web uniquement)
            if (httpContext.Request.Cookies.TryGetValue("SignalR-ConnectionId", out var connectionId))
            {
                return connectionId;
            }

            // Méthode 2 : Redis mapping (Mobile/API)
            var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Récupérer le ConnectionId depuis Redis (synchrone pour simplicité)
                return _connectionMapping.GetConnectionIdAsync(userId).GetAwaiter().GetResult();
            }

            return null;
        }
    }
}

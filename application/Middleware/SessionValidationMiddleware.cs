using Microsoft.AspNetCore.Identity;
using domain.Entities;
using domain.Interfaces.Services;

namespace application.Middleware
{
    /// <summary>
    /// Middleware qui vérifie si la session de l'utilisateur est toujours valide dans la DB
    /// Si la session est révoquée ou supprimée, déconnecte l'utilisateur
    /// </summary>
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionValidationMiddleware> _logger;

        public SessionValidationMiddleware(
            RequestDelegate next,
            ILogger<SessionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager)
        {
            // Vérifier si l'utilisateur est authentifié
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Récupérer le cookie ID ou session ID
                var userId = context.User.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Récupérer toutes les sessions actives de l'utilisateur
                    var activeSessions = await authService.GetActiveSessionsAsync(userId);

                    // Si l'utilisateur n'a plus de session active
                    if (!activeSessions.Any())
                    {
                        _logger.LogWarning("Session révoquée pour l'utilisateur {UserId}. Déconnexion forcée.", userId);
                        
                        // Déconnecter l'utilisateur
                        await signInManager.SignOutAsync();
                        
                        // Rediriger vers la page de login
                        context.Response.Redirect("/Auth/Login?sessionExpired=true");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}

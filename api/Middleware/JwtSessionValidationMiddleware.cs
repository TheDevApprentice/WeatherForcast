using domain.Interfaces.Services;

namespace api.Middleware
{
    /// <summary>
    /// Middleware qui vérifie si le JWT token existe toujours comme session valide dans la DB
    /// Si la session est révoquée ou supprimée, retourne 401 Unauthorized
    /// </summary>
    public class JwtSessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtSessionValidationMiddleware> _logger;

        public JwtSessionValidationMiddleware(
            RequestDelegate next,
            ILogger<JwtSessionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ISessionManagementService sessionManagementService)
        {
            // Vérifier si l'utilisateur est authentifié
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Récupérer le token JWT depuis le header Authorization
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    
                    // Vérifier si le token existe et est valide dans la DB
                    var isValid = await sessionManagementService.IsValidAsync(token);
                    
                    if (!isValid)
                    {
                        var userId = context.User.Claims
                            .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                        
                        _logger.LogWarning("Session JWT révoquée pour l'utilisateur {UserId}. Accès refusé.", userId);
                        
                        // Retourner 401 Unauthorized
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new 
                        { 
                            Message = "Session expirée ou révoquée. Veuillez vous reconnecter." 
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}

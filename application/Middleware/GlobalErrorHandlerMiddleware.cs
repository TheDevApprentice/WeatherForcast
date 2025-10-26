using domain.Events;
using domain.Exceptions;
using System.Security.Claims;

namespace application.Middleware
{
    /// <summary>
    /// Middleware global pour catcher toutes les exceptions non gérées
    /// Filet de sécurité pour éviter les erreurs 500 non tracées
    /// </summary>
    public class GlobalErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlerMiddleware> _logger;

        public GlobalErrorHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPublisher publisher)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                // ✅ Exception typée du domain - déjà gérée normalement
                _logger.LogWarning(ex, 
                    "[GlobalErrorHandler] DomainException non catchée | Type={ErrorType} | Action={Action}",
                    ex.ErrorType,
                    ex.Action);

                // ✅ Publier l'erreur pour notification temps réel
                var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    // await publisher.PublishDomainExceptionAsync(context.User, ex);
                }

                // Rediriger vers la page d'erreur avec message
                context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(ex.Message)}");
            }
            catch (Exception ex)
            {
                // ❌ Exception non gérée - Erreur critique
                _logger.LogError(ex, 
                    "[GlobalErrorHandler] Exception non gérée | Path={Path} | User={User}",
                    context.Request.Path,
                    context.User?.Identity?.Name ?? "Anonymous");

                // ⚠️ Publier l'erreur pour notification temps réel (COMMENTÉ)
                // Décommenter en production si vous voulez notifier l'utilisateur
                /*
                var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    await publisher.PublishGenericErrorAsync(
                        context.User,
                        "Une erreur inattendue est survenue. Veuillez réessayer.",
                        "Unknown",
                        null,
                        null,
                        ex);
                }
                */

                // Rediriger vers la page d'erreur générique
                context.Response.Redirect("/Home/Error");
            }
        }
    }

    /// <summary>
    /// Extension pour enregistrer le middleware
    /// </summary>
    public static class GlobalErrorHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalErrorHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalErrorHandlerMiddleware>();
        }
    }
}

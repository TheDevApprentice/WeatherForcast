using System.Security.Claims;
using System.Text;
using api.DTOs;
using domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace api.Middleware
{
    /// <summary>
    /// Middleware d'authentification par clé API (OAuth2 Client Credentials)
    /// Vérifie les en-têtes Authorization: Basic base64(key:secret)
    /// </summary>
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ApiKeyAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<ApiKeyAuthenticationMiddleware> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
        {
            // Skip les endpoints publics (Swagger, health check, etc.)
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
            if (path.Contains("/swagger") || 
                path.Contains("/health") || 
                path == "/" ||
                path == "/api")
            {
                await _next(context);
                return;
            }

            // Vérifier l'en-tête Authorization
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                await UnauthorizedResponse(context, "Missing Authorization header");
                return;
            }

            var authHeaderValue = authHeader.ToString();

            // Format attendu: "Basic base64(key:secret)"
            if (!authHeaderValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                await UnauthorizedResponse(context, "Invalid Authorization header format. Expected: Basic base64(key:secret)");
                return;
            }

            try
            {
                // Décoder le base64
                var encodedCredentials = authHeaderValue.Substring("Basic ".Length).Trim();
                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedCredentials.Split(':', 2);

                if (credentials.Length != 2)
                {
                    await UnauthorizedResponse(context, "Invalid credentials format");
                    return;
                }

                var key = credentials[0];
                var secret = credentials[1];

                // Valider la clé API
                var (isValid, apiKey) = await apiKeyService.ValidateApiKeyAsync(key, secret);

                if (!isValid || apiKey == null)
                {
                    _logger.LogWarning("Invalid API key attempt: {Key}", key);
                    await UnauthorizedResponse(context, "Invalid API key or secret");
                    return;
                }

                // Mettre à jour LastUsedAt dans un scope séparé (fire-and-forget thread-safe)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedApiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
                        await scopedApiKeyService.UpdateLastUsedAsync(key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update API key last used date: {Key}", key);
                    }
                });
                
                // Créer les claims pour l'utilisateur
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, apiKey.UserId),
                    new Claim(ClaimTypes.Email, apiKey.User?.Email ?? ""),
                    new Claim("ApiKey", apiKey.Key),
                    new Claim("Scopes", apiKey.Scopes)
                };

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);

                _logger.LogInformation("API key authenticated: {Key} for user {UserId}", 
                    apiKey.Key, apiKey.UserId);
            }
            catch (FormatException)
            {
                await UnauthorizedResponse(context, "Invalid base64 encoding");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                await UnauthorizedResponse(context, "Authentication error");
                return;
            }

            await _next(context);
        }

        private static async Task UnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            
            var response = new ErrorResponse
            {
                Error = "unauthorized",
                Message = message,
                Documentation = "https://localhost:7252/docs/authentication"
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    /// <summary>
    /// Extension pour enregistrer le middleware
    /// </summary>
    public static class ApiKeyAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        }
    }
}

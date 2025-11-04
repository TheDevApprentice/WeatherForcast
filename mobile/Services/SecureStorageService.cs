using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

namespace mobile.Services
{
    /// <summary>
    /// Service de stockage sécurisé utilisant SecureStorage de MAUI
    /// Inclut la validation JWT avec vérification d'expiration
    /// </summary>
    public class SecureStorageService : ISecureStorageService
    {
        private const string TOKEN_KEY = "jwt_token";
        private const string EMAIL_KEY = "user_email";
        private const string FIRSTNAME_KEY = "user_firstname";
        private const string LASTNAME_KEY = "user_lastname";
        
        private readonly ILogger<SecureStorageService> _logger;

        public SecureStorageService(ILogger<SecureStorageService> logger)
        {
            _logger = logger;
        }

        public async Task SaveTokenAsync(string token)
        {
            await SecureStorage.SetAsync(TOKEN_KEY, token);
#if DEBUG
            _logger.LogDebug("Token saved successfully");
#endif
        }

        public async Task<string?> GetTokenAsync()
        {
            var token = await SecureStorage.GetAsync(TOKEN_KEY);
#if DEBUG
            _logger.LogDebug("Token retrieved: {Status}", string.IsNullOrEmpty(token) ? "NULL/EMPTY" : "OK");
#endif
            return token;
        }

        public async Task RemoveTokenAsync()
        {
            SecureStorage.Remove(TOKEN_KEY);
            await Task.CompletedTask;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        /// <summary>
        /// Vérifie si le token JWT est valide (non expiré)
        /// </summary>
        public async Task<bool> IsTokenValidAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("Token is null or empty");
                    return false;
                }

                // Parser le token JWT
                var handler = new JwtSecurityTokenHandler();
                
                // Vérifier que c'est un token JWT valide
                if (!handler.CanReadToken(token))
                {
                    _logger.LogWarning("Token format is invalid");
                    return false;
                }

                var jwtToken = handler.ReadJwtToken(token);
                
                // Vérifier l'expiration (avec marge de 30 secondes)
                var expirationTime = jwtToken.ValidTo;
                var isExpired = expirationTime.AddSeconds(-30) <= DateTime.UtcNow;
                
                if (isExpired)
                {
                    _logger.LogInformation("Token expired at {ExpirationTime}", expirationTime);
                    return false;
                }
                
                _logger.LogDebug("Token is valid, expires at {ExpirationTime}", expirationTime);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT token");
                return false;
            }
        }

        public async Task SaveUserInfoAsync(string email, string firstName, string lastName)
        {
            Preferences.Set(EMAIL_KEY, email);
            Preferences.Set(FIRSTNAME_KEY, firstName);
            Preferences.Set(LASTNAME_KEY, lastName);
            await Task.CompletedTask;
        }

        public async Task<(string Email, string FirstName, string LastName)> GetUserInfoAsync()
        {
            var email = Preferences.Get(EMAIL_KEY, string.Empty);
            var firstName = Preferences.Get(FIRSTNAME_KEY, string.Empty);
            var lastName = Preferences.Get(LASTNAME_KEY, string.Empty);
            
            await Task.CompletedTask;
            return (email, firstName, lastName);
        }

        /// <summary>
        /// Extrait les informations utilisateur du token JWT (pour authentification offline)
        /// </summary>
        public async Task<(string UserId, string Email, string FirstName, string LastName)?> GetUserInfoFromTokenAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var handler = new JwtSecurityTokenHandler();
                
                if (!handler.CanReadToken(token))
                {
                    return null;
                }

                var jwtToken = handler.ReadJwtToken(token);
                
                // Extraire les claims
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid")?.Value ?? string.Empty;
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty;
                var firstName = jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? string.Empty;
                var lastName = jwtToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? string.Empty;
                
                _logger.LogDebug("Extracted user info from token: UserId={UserId}, Email={Email}", userId, email);
                
                return (userId, email, firstName, lastName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user info from JWT token");
                return null;
            }
        }

        public async Task ClearAllAsync()
        {
            SecureStorage.Remove(TOKEN_KEY);
            Preferences.Remove(EMAIL_KEY);
            Preferences.Remove(FIRSTNAME_KEY);
            Preferences.Remove(LASTNAME_KEY);
            await Task.CompletedTask;
        }
    }
}

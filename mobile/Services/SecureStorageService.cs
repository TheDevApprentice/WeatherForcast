using Microsoft.Extensions.Logging;

namespace mobile.Services
{
    /// <summary>
    /// Service de stockage sécurisé utilisant SecureStorage de MAUI
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
            _logger.LogDebug("Token saved to SecureStorage");
#endif
        }

        public async Task<string?> GetTokenAsync()
        {
            var token = await SecureStorage.GetAsync(TOKEN_KEY);
            
#if DEBUG
            _logger.LogDebug(
                "Token retrieved from SecureStorage: {Status}",
                string.IsNullOrEmpty(token) ? "NULL/EMPTY" : $"{token.Substring(0, Math.Min(30, token.Length))}...");
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

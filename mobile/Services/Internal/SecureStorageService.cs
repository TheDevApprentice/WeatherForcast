using Microsoft.Extensions.Logging;
using mobile.Services.Internal.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace mobile.Services.Internal
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

        public SecureStorageService ()
        {
        }

        public async Task SaveTokenAsync (string token)
        {
            try
            {
                await SecureStorage.SetAsync(TOKEN_KEY, token);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur SaveTokenAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                throw;
            }
        }

        public async Task<string?> GetTokenAsync ()
        {
            try
            {
                var token = await SecureStorage.GetAsync(TOKEN_KEY);
                return token;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur GetTokenAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return null;
            }
        }

        public async Task RemoveTokenAsync ()
        {
            try
            {
                SecureStorage.Remove(TOKEN_KEY);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur RemoveTokenAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        public async Task<bool> IsAuthenticatedAsync ()
        {
            try
            {
                var token = await GetTokenAsync();
                return !string.IsNullOrEmpty(token);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur IsAuthenticatedAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return false;
            }
        }

        /// <summary>
        /// Vérifie si le token JWT est valide (non expiré)
        /// </summary>
        public async Task<bool> IsTokenValidAsync ()
        {
            try
            {
                var token = await GetTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // Parser le token JWT
                var handler = new JwtSecurityTokenHandler();

                // Vérifier que c'est un token JWT valide
                if (!handler.CanReadToken(token))
                {
                    return false;
                }

                var jwtToken = handler.ReadJwtToken(token);

                // Vérifier l'expiration (avec marge de 30 secondes)
                var expirationTime = jwtToken.ValidTo;
                var isExpired = expirationTime.AddSeconds(-30) <= DateTime.UtcNow;

                if (isExpired)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur IsTokenValidAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return false;
            }
        }

        public async Task SaveUserInfoAsync (string email, string firstName, string lastName)
        {
            try
            {
                Preferences.Set(EMAIL_KEY, email);
                Preferences.Set(FIRSTNAME_KEY, firstName);
                Preferences.Set(LASTNAME_KEY, lastName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur SaveUserInfoAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                throw;
            }
        }

        public async Task<(string Email, string FirstName, string LastName)> GetUserInfoAsync ()
        {
            try
            {
                var email = Preferences.Get(EMAIL_KEY, string.Empty);
                var firstName = Preferences.Get(FIRSTNAME_KEY, string.Empty);
                var lastName = Preferences.Get(LASTNAME_KEY, string.Empty);

                await Task.CompletedTask;
                return (email, firstName, lastName);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur GetUserInfoAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return (string.Empty, string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Extrait les informations utilisateur du token JWT (pour authentification offline)
        /// </summary>
        public async Task<(string UserId, string Email, string FirstName, string LastName)?> GetUserInfoFromTokenAsync ()
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

                return (userId, email, firstName, lastName);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur GetUserInfoFromTokenAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return null;
            }
        }
        /// <summary>
        /// Extrait les informations utilisateur du token JWT (pour authentification offline)
        /// </summary>
        public async Task<string?> GetUserIdFromTokenAsync ()
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

                return (userId);
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur GetUserIdFromTokenAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                return null;
            }
        }

        public async Task ClearAllAsync ()
        {
            try
            {
                try { SecureStorage.Remove(TOKEN_KEY); }
                catch (Exception ex) {  
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur suppression TOKEN_KEY: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }

                try { Preferences.Remove(EMAIL_KEY); }
                catch (Exception ex) { 
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur suppression EMAIL_KEY: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }

                try { Preferences.Remove(FIRSTNAME_KEY); }
                catch (Exception ex) { 
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur suppression FIRSTNAME_KEY: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }

                try { Preferences.Remove(LASTNAME_KEY); }
                catch (Exception ex) { 
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur suppression LASTNAME_KEY: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SecureStorage", $"Erreur ClearAllAsync: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }
    }
}

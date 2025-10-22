using domain.Entities;
using domain.Interfaces.Repositories;
using domain.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace domain.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IApiKeyRepository _apiKeyRepository;

        public ApiKeyService(IApiKeyRepository apiKeyRepository)
        {
            _apiKeyRepository = apiKeyRepository;
        }

        public async Task<(ApiKey apiKey, string plainSecret)> GenerateApiKeyAsync(
            string userId,
            string name,
            int? expirationDays = null)
        {
            // Générer la clé (client_id)
            var key = $"wf_live_{GenerateRandomString(32)}";

            // Générer le secret (client_secret)
            var plainSecret = $"wf_secret_{GenerateRandomString(48)}";

            // Hasher le secret
            var secretHash = HashSecret(plainSecret);

            var apiKey = new ApiKey
            {
                Name = name,
                Key = key,
                SecretHash = secretHash,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expirationDays.HasValue
                    ? DateTime.UtcNow.AddDays(expirationDays.Value)
                    : null,
                IsActive = true,
                Scopes = "read" // Par défaut, lecture seule
            };

            await _apiKeyRepository.CreateAsync(apiKey);

            return (apiKey, plainSecret);
        }

        public async Task<(bool isValid, ApiKey? apiKey)> ValidateApiKeyAsync(string key, string secret)
        {
            var apiKey = await _apiKeyRepository.GetByKeyAsync(key);

            if (apiKey == null)
            {
                return (false, null);
            }

            // Vérifier si la clé est active
            if (!apiKey.IsActive)
            {
                return (false, null);
            }

            // Vérifier si la clé est expirée
            if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            {
                return (false, null);
            }

            // Vérifier le secret
            var secretHash = HashSecret(secret);
            if (secretHash != apiKey.SecretHash)
            {
                return (false, null);
            }

            return (true, apiKey);
        }

        public async Task<IEnumerable<ApiKey>> GetUserApiKeysAsync(string userId)
        {
            return await _apiKeyRepository.GetByUserIdAsync(userId);
        }

        public async Task<bool> RevokeApiKeyAsync(int apiKeyId, string userId)
        {
            return await _apiKeyRepository.RevokeAsync(apiKeyId, userId);
        }

        public async Task UpdateLastUsedAsync(string key)
        {
            await _apiKeyRepository.IncrementRequestCountAsync(key);
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }

            var result = new StringBuilder(length);
            foreach (var b in random)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }

        private string HashSecret(string secret)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(secret);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}

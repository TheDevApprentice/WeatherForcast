using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Services;
using domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace domain.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ApiKeyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            // Créer l'API Key avec le constructeur (encapsulation)
            var expiresAt = expirationDays.HasValue
                ? DateTime.UtcNow.AddDays(expirationDays.Value)
                : (DateTime?)null;

            var apiKey = new ApiKey(
                name: name,
                key: key,
                secretHash: secretHash,
                userId: userId,
                scopes: ApiKeyScopes.ReadWrite, // Par défaut, lecture et écriture
                expiresAt: expiresAt
            );

            await _unitOfWork.ApiKeys.CreateAsync(apiKey);
            // SaveChangesAsync est déjà appelé dans CreateAsync du repository

            return (apiKey, plainSecret);
        }

        public async Task<(bool isValid, ApiKey? apiKey)> ValidateApiKeyAsync(string key, string secret)
        {
            var apiKey = await _unitOfWork.ApiKeys.GetByKeyAsync(key);

            if (apiKey == null)
            {
                return (false, null);
            }

            // Utiliser la méthode IsValid() de l'entité
            if (!apiKey.IsValid())
            {
                return (false, null);
            }

            // Vérifier le secret
            var secretHash = HashSecret(secret);
            if (secretHash != apiKey.SecretHash)
            {
                return (false, null);
            }

            // Enregistrer l'utilisation
            apiKey.RecordUsage();
            await _unitOfWork.SaveChangesAsync();

            return (true, apiKey);
        }

        public async Task<IEnumerable<ApiKey>> GetUserApiKeysAsync(string userId)
        {
            return await _unitOfWork.ApiKeys.GetByUserIdAsync(userId);
        }

        public async Task<bool> RevokeApiKeyAsync(int apiKeyId, string userId, string reason)
        {
            var apiKey = await _unitOfWork.ApiKeys.GetByIdAsync(apiKeyId);
            
            if (apiKey == null || apiKey.UserId != userId)
            {
                return false;
            }

            // Utiliser la méthode Revoke() de l'entité (avec traçabilité)
            apiKey.Revoke(reason);
            await _unitOfWork.SaveChangesAsync();
            
            return true;
        }

        public async Task UpdateLastUsedAsync(string key)
        {
            // Cette méthode est maintenant gérée par RecordUsage() dans ValidateApiKeyAsync
            // On peut la garder pour compatibilité ou la supprimer
            var apiKey = await _unitOfWork.ApiKeys.GetByKeyAsync(key);
            if (apiKey != null)
            {
                apiKey.RecordUsage();
                await _unitOfWork.SaveChangesAsync();
            }
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

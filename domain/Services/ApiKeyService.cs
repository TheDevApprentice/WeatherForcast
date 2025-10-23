using domain.Entities;
using domain.Events.Admin;
using domain.Interfaces;
using domain.Interfaces.Services;
using domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace domain.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApiKeyService(
            IUnitOfWork unitOfWork,
            IPublisher publisher,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _userManager = userManager;
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
            await _unitOfWork.SaveChangesAsync();

            // Publier l'événement ApiKeyCreated
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _publisher.Publish(new ApiKeyCreatedEvent(
                    apiKeyId: apiKey.Id,
                    userId: userId,
                    email: user.Email!,
                    keyName: name,
                    expiresAt: expiresAt
                ));
            }

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

            // Vérifier le secret avec bcrypt
            if (!VerifySecret(secret, apiKey.SecretHash))
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

            // Publier l'événement ApiKeyRevoked
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _publisher.Publish(new ApiKeyRevokedEvent(
                    apiKeyId: apiKey.Id,
                    userId: userId,
                    email: user.Email!,
                    keyName: apiKey.Name,
                    revokedBy: user.Email
                ));
            }

            return true;
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

        /// <summary>
        /// Hache un secret avec Argon2id (recommandé OWASP 2024)
        /// Paramètres: 64 MB RAM, 4 itérations, 8 threads
        /// Résistant aux attaques GPU, ASIC et side-channel
        /// </summary>
        private string HashSecret(string secret)
        {
            // Générer un salt aléatoire de 16 bytes
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Configurer Argon2id avec paramètres recommandés OWASP
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(secret)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 8;      // 8 threads
                argon2.MemorySize = 65536;           // 64 MB de RAM
                argon2.Iterations = 4;               // 4 itérations

                var hash = argon2.GetBytes(32);      // Hash de 32 bytes

                // Combiner salt + hash pour stockage (16 + 32 = 48 bytes)
                var hashWithSalt = new byte[48];
                Buffer.BlockCopy(salt, 0, hashWithSalt, 0, 16);
                Buffer.BlockCopy(hash, 0, hashWithSalt, 16, 32);

                return Convert.ToBase64String(hashWithSalt);
            }
        }

        /// <summary>
        /// Vérifie un secret contre son hash Argon2id
        /// Utilise une comparaison constant-time pour éviter les timing attacks
        /// </summary>
        private bool VerifySecret(string secret, string hashWithSaltBase64)
        {
            try
            {
                var hashWithSalt = Convert.FromBase64String(hashWithSaltBase64);

                // Vérifier la longueur (16 bytes salt + 32 bytes hash)
                if (hashWithSalt.Length != 48)
                    return false;

                // Extraire le salt (16 premiers bytes)
                var salt = new byte[16];
                Buffer.BlockCopy(hashWithSalt, 0, salt, 0, 16);

                // Extraire le hash stocké (32 bytes suivants)
                var storedHash = new byte[32];
                Buffer.BlockCopy(hashWithSalt, 16, storedHash, 0, 32);

                // Re-hasher le secret avec le même salt et paramètres
                using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(secret)))
                {
                    argon2.Salt = salt;
                    argon2.DegreeOfParallelism = 8;
                    argon2.MemorySize = 65536;
                    argon2.Iterations = 4;

                    var newHash = argon2.GetBytes(32);

                    // Comparaison constant-time pour éviter timing attacks
                    return CryptographicOperations.FixedTimeEquals(storedHash, newHash);
                }
            }
            catch
            {
                // Hash invalide, corrompu ou format incorrect
                return false;
            }
        }
    }
}

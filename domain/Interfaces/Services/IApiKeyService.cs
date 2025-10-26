using domain.Entities;

namespace domain.Interfaces.Services
{
    public interface IApiKeyService
    {
        /// <summary>
        /// Générer une nouvelle clé API pour un utilisateur
        /// </summary>
        /// <returns>Tuple (apiKey, plainSecret) - Le secret n'est retourné qu'UNE seule fois</returns>
        Task<(ApiKey apiKey, string plainSecret)> GenerateApiKeyAsync(string userId, string name, int? expirationDays = null);
        
        /// <summary>
        /// Valider une clé API et son secret
        /// </summary>
        Task<(bool isValid, ApiKey? apiKey)> ValidateApiKeyAsync(string key, string secret);
        
        /// <summary>
        /// Récupérer toutes les clés d'un utilisateur
        /// </summary>
        Task<IEnumerable<ApiKey>> GetUserApiKeysAsync(string userId);
        
        /// <summary>
        /// Révoquer une clé API
        /// </summary>
        Task<bool> RevokeApiKeyAsync(int apiKeyId, string userId, string reason);
    }
}

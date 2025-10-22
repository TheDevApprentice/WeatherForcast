using domain.Entities;

namespace domain.Interfaces.Repositories
{
    public interface IApiKeyRepository
    {
        Task<ApiKey?> GetByKeyAsync(string key);
        Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId);
        Task<ApiKey> CreateAsync(ApiKey apiKey);
        Task<bool> UpdateAsync(ApiKey apiKey);
        Task<bool> RevokeAsync(int id, string userId);
        Task<bool> IncrementRequestCountAsync(string key);
    }
}

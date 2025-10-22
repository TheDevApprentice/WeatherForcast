using domain.Entities;
using domain.Interfaces.Repositories;
using infra.Data;
using Microsoft.EntityFrameworkCore;

namespace infra.Repositories
{
    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly AppDbContext _context;

        public ApiKeyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiKey?> GetByKeyAsync(string key)
        {
            return await _context.ApiKeys
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.Key == key && k.IsActive);
        }

        public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId)
        {
            return await _context.ApiKeys
                .Where(k => k.UserId == userId)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<ApiKey> CreateAsync(ApiKey apiKey)
        {
            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();
            return apiKey;
        }

        public async Task<bool> UpdateAsync(ApiKey apiKey)
        {
            _context.ApiKeys.Update(apiKey);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> RevokeAsync(int id, string userId)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);

            if (apiKey == null)
            {
                return false;
            }

            apiKey.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IncrementRequestCountAsync(string key)
        {
            var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(k => k.Key == key);
            
            if (apiKey == null)
            {
                return false;
            }

            apiKey.RequestCount++;
            apiKey.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

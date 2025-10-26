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
            await _context.ApiKeys.AddAsync(apiKey);
            // SaveChanges géré par le UnitOfWork
            return apiKey;
        }

        public Task<bool> UpdateAsync(ApiKey apiKey)
        {
            _context.ApiKeys.Update(apiKey);
            // SaveChanges géré par le UnitOfWork
            return Task.FromResult(true);
        }

        public async Task<ApiKey?> GetByIdAsync(int id)
        {
            return await _context.ApiKeys
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.Id == id);
        }
    }
}

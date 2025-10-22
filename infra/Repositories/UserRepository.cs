using Microsoft.EntityFrameworkCore;
using domain.Entities;
using infra.Data;
using domain.Interfaces.Repositories;

namespace infra.Repositories
{
    /// <summary>
    /// Implémentation du repository pour les utilisateurs
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.UserSessions)
                .ThenInclude(us => us.Session)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserSessions)
                .ThenInclude(us => us.Session)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.UserSessions)
                .ToListAsync();
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RecordLogin(); // Utilise la méthode de l'entité
                // SaveChanges géré par le UnitOfWork
            }
        }

        public async Task<bool> SetActiveStatusAsync(string userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Utilise les méthodes de l'entité
            if (isActive && !user.IsActive)
            {
                user.Reactivate();
            }
            else if (!isActive && user.IsActive)
            {
                user.Deactivate("Désactivé par un administrateur");
            }
            // SaveChanges géré par le UnitOfWork
            return true;
        }

        public async Task<IEnumerable<Session>> GetActiveSessionsAsync(string userId)
        {
            return await _context.UserSessions
                .Where(us => us.UserId == userId)
                .Select(us => us.Session)
                .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }
    }
}

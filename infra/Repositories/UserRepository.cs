using Microsoft.EntityFrameworkCore;
using domain.Entities;
using domain.DTOs;
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

        public async Task<PagedResult<ApplicationUser>> SearchUsersAsync(UserSearchCriteria criteria)
        {
            var query = _context.Users.AsQueryable();

            // Recherche textuelle (nom, prénom, email)
            if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
            {
                var searchTerm = criteria.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Email!.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm));
            }

            // Filtre par statut actif/inactif
            if (criteria.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == criteria.IsActive.Value);
            }

            // Filtre par date de création
            if (criteria.CreatedAfter.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= criteria.CreatedAfter.Value);
            }

            if (criteria.CreatedBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= criteria.CreatedBefore.Value);
            }

            // Filtre par dernière connexion
            if (criteria.LastLoginAfter.HasValue)
            {
                query = query.Where(u => u.LastLoginAt >= criteria.LastLoginAfter.Value);
            }

            if (criteria.LastLoginBefore.HasValue)
            {
                query = query.Where(u => u.LastLoginAt <= criteria.LastLoginBefore.Value);
            }

            // Compter le total AVANT la pagination
            var totalCount = await query.CountAsync();

            // Tri
            query = criteria.SortBy?.ToLower() switch
            {
                "email" => criteria.SortDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "firstname" => criteria.SortDescending
                    ? query.OrderByDescending(u => u.FirstName)
                    : query.OrderBy(u => u.FirstName),
                "lastname" => criteria.SortDescending
                    ? query.OrderByDescending(u => u.LastName)
                    : query.OrderBy(u => u.LastName),
                "lastloginat" => criteria.SortDescending
                    ? query.OrderByDescending(u => u.LastLoginAt)
                    : query.OrderBy(u => u.LastLoginAt),
                _ => criteria.SortDescending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt)
            };

            // Pagination
            var items = await query
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync();

            return new PagedResult<ApplicationUser>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = criteria.PageNumber,
                PageSize = criteria.PageSize
            };
        }
    }
}

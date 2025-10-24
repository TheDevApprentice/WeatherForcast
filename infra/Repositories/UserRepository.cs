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
            var query = _context.Users.AsNoTracking().AsQueryable();

            // Recherche textuelle optimisée (nom, prénom, email)
            // Utilisation de EF.Functions.Like pour meilleure performance SQL
            if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
            {
                var searchTerm = criteria.SearchTerm.Trim();
                query = query.Where(u =>
                    EF.Functions.Like(u.Email!, $"%{searchTerm}%") ||
                    EF.Functions.Like(u.FirstName, $"%{searchTerm}%") ||
                    EF.Functions.Like(u.LastName, $"%{searchTerm}%"));
            }

            // Filtres optimisés avec switch expression
            query = criteria.IsActive switch
            {
                true => query.Where(u => u.IsActive),
                false => query.Where(u => !u.IsActive),
                _ => query
            };

            // Filtre par date de création (range optimisé)
            if (criteria.CreatedAfter.HasValue && criteria.CreatedBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= criteria.CreatedAfter.Value 
                                      && u.CreatedAt <= criteria.CreatedBefore.Value);
            }
            else if (criteria.CreatedAfter.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= criteria.CreatedAfter.Value);
            }
            else if (criteria.CreatedBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= criteria.CreatedBefore.Value);
            }

            // Filtre par dernière connexion (range optimisé)
            if (criteria.LastLoginAfter.HasValue && criteria.LastLoginBefore.HasValue)
            {
                query = query.Where(u => u.LastLoginAt >= criteria.LastLoginAfter.Value 
                                      && u.LastLoginAt <= criteria.LastLoginBefore.Value);
            }
            else if (criteria.LastLoginAfter.HasValue)
            {
                query = query.Where(u => u.LastLoginAt >= criteria.LastLoginAfter.Value);
            }
            else if (criteria.LastLoginBefore.HasValue)
            {
                query = query.Where(u => u.LastLoginAt <= criteria.LastLoginBefore.Value);
            }

            // Tri optimisé avec switch expression
            query = (criteria.SortBy?.ToLowerInvariant(), criteria.SortDescending) switch
            {
                ("email", true) => query.OrderByDescending(u => u.Email),
                ("email", false) => query.OrderBy(u => u.Email),
                ("firstname", true) => query.OrderByDescending(u => u.FirstName),
                ("firstname", false) => query.OrderBy(u => u.FirstName),
                ("lastname", true) => query.OrderByDescending(u => u.LastName),
                ("lastname", false) => query.OrderBy(u => u.LastName),
                ("lastloginat", true) => query.OrderByDescending(u => u.LastLoginAt),
                ("lastloginat", false) => query.OrderBy(u => u.LastLoginAt),
                (_, true) => query.OrderByDescending(u => u.CreatedAt),
                _ => query.OrderBy(u => u.CreatedAt)
            };

            // IMPORTANT: EF Core interdit plusieurs opérations concurrentes sur le même DbContext.
            // Exécuter séquentiellement Count puis la pagination pour éviter InvalidOperationException.
            var totalCount = await query.CountAsync();
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

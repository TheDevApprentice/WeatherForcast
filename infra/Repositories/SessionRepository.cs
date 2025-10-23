using Microsoft.EntityFrameworkCore;
using domain.Entities;
using infra.Data;
using domain.Interfaces.Repositories;

namespace infra.Repositories
{
    /// <summary>
    /// Implémentation du repository pour les sessions
    /// </summary>
    public class SessionRepository : ISessionRepository
    {
        private readonly AppDbContext _context;

        public SessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Session> CreateAsync(Session session)
        {
            await _context.Sessions.AddAsync(session);
            // SaveChanges géré par le UnitOfWork
            return session;
        }

        public async Task<Session> CreateSessionWithUserAsync(Session session, string userId)
        {
            await _context.Sessions.AddAsync(session);

            // Utiliser le constructeur public de UserSession
            var userSession = new UserSession(userId, session.Id);

            await _context.UserSessions.AddAsync(userSession);
            // SaveChanges géré par le UnitOfWork
            return session;
        }

        public async Task<Session?> GetByTokenAsync(string token)
        {
            return await _context.Sessions
                .Include(s => s.UserSessions)
                .ThenInclude(us => us.User)
                .FirstOrDefaultAsync(s => s.Token == token);
        }

        public async Task<Session?> GetByIdAsync(Guid sessionId)
        {
            return await _context.Sessions
                .Include(s => s.UserSessions)
                .ThenInclude(us => us.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(string userId)
        {
            return await _context.UserSessions
                .Where(us => us.UserId == userId)
                .Select(us => us.Session)
                .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<bool> RevokeAsync(Guid sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Revoke(); // Utilise la méthode de l'entité
            // SaveChanges géré par le UnitOfWork
            return true;
        }

        public async Task<bool> DeleteAsync(Guid sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return false;

            // Suppression physique (cascade delete sur UserSessions via EF Core)
            _context.Sessions.Remove(session);
            // SaveChanges géré par le UnitOfWork
            return true;
        }

        public async Task<int> RevokeAllByUserIdAsync(string userId)
        {
            var sessions = await _context.UserSessions
                .Where(us => us.UserId == userId)
                .Select(us => us.Session)
                .Where(s => !s.IsRevoked)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.Revoke(); // Utilise la méthode de l'entité
            }
            // SaveChanges géré par le UnitOfWork
            return sessions.Count;
        }

        public async Task<bool> IsValidAsync(string token)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Token == token);

            if (session == null) return false;

            return !session.IsRevoked && session.ExpiresAt > DateTime.UtcNow;
        }

        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.Sessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow && !s.IsRevoked)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.Revoke("Session expirée - Nettoyage automatique"); // Utilise la méthode de l'entité
            }
            // SaveChanges géré par le UnitOfWork
            return expiredSessions.Count;
        }
    }
}

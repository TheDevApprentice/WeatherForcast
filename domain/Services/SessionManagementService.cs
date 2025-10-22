using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Services;

namespace domain.Services
{
    /// <summary>
    /// Service de gestion du cycle de vie des sessions
    /// Responsabilité : CRUD sessions uniquement
    /// </summary>
    public class SessionManagementService : ISessionManagementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SessionManagementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Session> CreateWebSessionAsync(
            string userId,
            string cookieId,
            string? ipAddress = null,
            string? userAgent = null,
            int expirationDays = 7)
        {
            var expiresAt = DateTime.UtcNow.AddDays(expirationDays);
            var session = new Session(cookieId, SessionType.Web, expiresAt, ipAddress, userAgent);

            await _unitOfWork.Sessions.CreateSessionWithUserAsync(session, userId);
            await _unitOfWork.SaveChangesAsync();
            return session;
        }

        public async Task<Session> CreateApiSessionAsync(
            string userId,
            string jwtToken,
            string? ipAddress = null,
            string? userAgent = null,
            int expirationHours = 24)
        {
            var expiresAt = DateTime.UtcNow.AddHours(expirationHours);
            var session = new Session(jwtToken, SessionType.Api, expiresAt, ipAddress, userAgent);

            await _unitOfWork.Sessions.CreateSessionWithUserAsync(session, userId);
            await _unitOfWork.SaveChangesAsync();
            return session;
        }

        public async Task<bool> RevokeAsync(Guid sessionId)
        {
            var result = await _unitOfWork.Sessions.RevokeAsync(sessionId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<int> RevokeAllByUserIdAsync(string userId)
        {
            var result = await _unitOfWork.Sessions.RevokeAllByUserIdAsync(userId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<bool> DeleteAsync(Guid sessionId)
        {
            var result = await _unitOfWork.Sessions.DeleteAsync(sessionId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<bool> IsValidAsync(string token)
        {
            return await _unitOfWork.Sessions.IsValidAsync(token);
        }

        public async Task<IEnumerable<Session>> GetActiveSessionsAsync(string userId)
        {
            return await _unitOfWork.Sessions.GetActiveSessionsByUserIdAsync(userId);
        }
    }
}

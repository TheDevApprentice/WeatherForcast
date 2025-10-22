using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace domain.Services
{
    /// <summary>
    /// Service d'authentification centralisé
    /// Utilisé par Web (cookies) et API (JWT)
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Success, string[] Errors, ApplicationUser? User)> RegisterAsync(
            string email,
            string password,
            string firstName,
            string lastName)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                return (true, Array.Empty<string>(), user);
            }

            var errors = result.Errors.Select(e => e.Description).ToArray();
            return (false, errors, null);
        }

        public async Task<(bool Success, ApplicationUser? User)> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return (false, null);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return (true, user);
            }

            return (false, null);
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<(bool Success, string[] Errors, ApplicationUser? User)> RegisterWithSessionAsync(
            string email,
            string password,
            string firstName,
            string lastName,
            string sessionToken,
            string? ipAddress = null,
            string? userAgent = null,
            bool isApiSession = false,
            int expirationHours = 24)
        {
            // 1. Créer l'utilisateur
            var (success, errors, user) = await RegisterAsync(email, password, firstName, lastName);

            if (!success || user == null)
            {
                return (false, errors, null);
            }

            // 2. Créer la session (qui fait son propre SaveChanges)
            if (isApiSession)
            {
                await this.CreateApiSessionAsync(user.Id, sessionToken, ipAddress, userAgent, expirationHours);
            }
            else
            {
                await this.CreateWebSessionAsync(user.Id, sessionToken, ipAddress, userAgent, expirationHours);
            }

            return (true, Array.Empty<string>(), user);
        }

        public async Task<(bool Success, ApplicationUser? User)> LoginWithSessionAsync(
            string email,
            string password,
            string sessionToken,
            string? ipAddress = null,
            string? userAgent = null,
            bool isApiSession = false,
            int expirationHours = 24)
        {
            // 1. Valider les credentials
            var (success, user) = await ValidateCredentialsAsync(email, password);

            if (!success || user == null)
            {
                return (false, null);
            }

            // 2. Révoquer toutes les anciennes sessions de cet utilisateur (évite les doublons)
            await this.RevokeAllUserSessionsAsync(user.Id);

            // 3. Créer la nouvelle session (qui fait son propre SaveChanges)
            if (isApiSession)
            {
                await this.CreateApiSessionAsync(user.Id, sessionToken, ipAddress, userAgent, expirationHours);
            }
            else
            {
                await this.CreateWebSessionAsync(user.Id, sessionToken, ipAddress, userAgent, expirationHours);
            }

            // 4. Mettre à jour LastLoginAt
            await _unitOfWork.Users.UpdateLastLoginAsync(user.Id);
            await _unitOfWork.SaveChangesAsync();

            return (true, user);
        }

        public async Task CreateWebSessionWithLastLoginUpdateAsync(
            string userId,
            string sessionToken,
            string? ipAddress = null,
            string? userAgent = null,
            int expirationDays = 7)
        {
            // 1. Révoquer toutes les anciennes sessions de cet utilisateur (évite les doublons)
            await this.RevokeAllUserSessionsAsync(userId);

            // 2. Créer la nouvelle session Web (qui fait son propre SaveChanges)
            await this.CreateWebSessionAsync(userId, sessionToken, ipAddress, userAgent, expirationDays);

            // 3. Mettre à jour LastLoginAt
            await _unitOfWork.Users.UpdateLastLoginAsync(userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Session> CreateWebSessionAsync(
            string userId,
            string cookieId,
            string? ipAddress = null,
            string? userAgent = null,
            int expirationDays = 7)
        {
            var session = new Session
            {
                Id = Guid.NewGuid(),
                Token = cookieId,
                Type = SessionType.Web,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

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
            var session = new Session
            {
                Id = Guid.NewGuid(),
                Token = jwtToken,
                Type = SessionType.Api,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _unitOfWork.Sessions.CreateSessionWithUserAsync(session, userId);
            await _unitOfWork.SaveChangesAsync();
            return session;
        }

        public async Task<bool> RevokeSessionAsync(Guid sessionId)
        {
            var result = await _unitOfWork.Sessions.RevokeAsync(sessionId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<bool> DeleteSessionAsync(Guid sessionId)
        {
            var result = await _unitOfWork.Sessions.DeleteAsync(sessionId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<int> RevokeAllUserSessionsAsync(string userId)
        {
            var result = await _unitOfWork.Sessions.RevokeAllByUserIdAsync(userId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<bool> IsSessionValidAsync(string token)
        {
            return await _unitOfWork.Sessions.IsValidAsync(token);
        }

        public async Task<IEnumerable<Session>> GetActiveSessionsAsync(string userId)
        {
            return await _unitOfWork.Sessions.GetActiveSessionsByUserIdAsync(userId);
        }
    }
}

using domain.Entities;
using domain.Events.Admin;
using domain.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace domain.Services
{
    /// <summary>
    /// Service d'orchestration de l'authentification
    /// Responsabilité : Coordonner UserManagement et SessionManagement pour Login/Register
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserManagementService _userManagementService;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly IPublisher _publisher;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(
            SignInManager<ApplicationUser> signInManager,
            IUserManagementService userManagementService,
            ISessionManagementService sessionManagementService,
            IPublisher publisher,
            IHttpContextAccessor httpContextAccessor)
        {
            _signInManager = signInManager;
            _userManagementService = userManagementService;
            _sessionManagementService = sessionManagementService;
            _publisher = publisher;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, ApplicationUser? User)> ValidateCredentialsAsync(
            string email,
            string password)
        {
            var user = await _userManagementService.GetByEmailAsync(email);
            if (user == null)
            {
                return (false, null);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user, password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return (true, user);
            }

            return (false, null);
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
            var (success, errors, user) = await _userManagementService.RegisterAsync(
                email, password, firstName, lastName);

            if (!success || user == null)
            {
                return (false, errors, null);
            }

            // 2. Créer la session
            if (isApiSession)
            {
                await _sessionManagementService.CreateApiSessionAsync(
                    user.Id, sessionToken, ipAddress, userAgent, expirationHours);
            }
            else
            {
                var expirationDays = expirationHours / 24;
                await _sessionManagementService.CreateWebSessionAsync(
                    user.Id, sessionToken, ipAddress, userAgent, expirationDays);
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

            // 2. Révoquer toutes les anciennes sessions
            await _sessionManagementService.RevokeAllByUserIdAsync(user.Id);

            // 3. Créer la nouvelle session
            if (isApiSession)
            {
                await _sessionManagementService.CreateApiSessionAsync(
                    user.Id, sessionToken, ipAddress, userAgent, expirationHours);
            }
            else
            {
                var expirationDays = expirationHours / 24;
                await _sessionManagementService.CreateWebSessionAsync(
                    user.Id, sessionToken, ipAddress, userAgent, expirationDays);
            }

            // 4. Mettre à jour LastLoginAt
            await _userManagementService.UpdateLastLoginAsync(user.Id);

            // 5. Publier l'événement UserLoggedIn
            await _publisher.Publish(new UserLoggedInEvent(
                userId: user.Id,
                email: user.Email!,
                userName: user.UserName,
                ipAddress: ipAddress,
                userAgent: userAgent
            ));

            return (true, user);
        }
    }
}

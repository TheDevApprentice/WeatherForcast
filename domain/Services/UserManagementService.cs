using domain.DTOs;
using domain.Entities;
using domain.Events;
using domain.Events.Admin;
using domain.Interfaces;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace domain.Services
{
    /// <summary>
    /// Service de gestion du cycle de vie des utilisateurs
    /// Responsabilité : CRUD utilisateurs uniquement
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IPublisher publisher,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string[] Errors, ApplicationUser? User)> RegisterAsync(
            string email,
            string password,
            string firstName,
            string lastName)
        {
            // Utilise le constructeur avec validation
            var user = new ApplicationUser(email, firstName, lastName);

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Publier l'événement UserRegistered
                var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                await _publisher.Publish(new UserRegisteredEvent(
                    userId: user.Id,
                    email: user.Email!,
                    userName: user.UserName,
                    ipAddress: ipAddress
                ));

                return (true, Array.Empty<string>(), user);
            }

            var errors = result.Errors.Select(e => e.Description).ToArray();
            return (false, errors, null);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            await _unitOfWork.Users.UpdateLastLoginAsync(userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PagedResult<ApplicationUser>> SearchUsersAsync(UserSearchCriteria criteria)
        {
            return await _unitOfWork.Users.SearchUsersAsync(criteria);
        }
    }
}

using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Services;
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

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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
    }
}

using domain.Constants;
using domain.Entities;
using domain.Events;
using domain.Events.Admin;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace domain.Services
{
    /// <summary>
    /// Service de gestion des rôles et claims
    /// </summary>
    public class RoleManagementService : IRoleManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IPublisher _publisher;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoleManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IPublisher publisher,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _publisher = publisher;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (!await _roleManager.RoleExistsAsync(roleName))
                return false;

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                // Publier l'événement UserRoleChanged
                var changedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
                await _publisher.Publish(new UserRoleChangedEvent(
                    userId: user.Id,
                    email: user.Email!,
                    roleName: roleName,
                    isAdded: true,
                    changedBy: changedBy
                ));
            }

            return result.Succeeded;
        }

        public async Task<bool> RemoveRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                // Publier l'événement UserRoleChanged
                var changedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
                await _publisher.Publish(new UserRoleChangedEvent(
                    userId: user.Id,
                    email: user.Email!,
                    roleName: roleName,
                    isAdded: false,
                    changedBy: changedBy
                ));
            }

            return result.Succeeded;
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> IsInRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<bool> AddClaimAsync(string userId, string claimType, string claimValue)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var claim = new Claim(claimType, claimValue);
            var result = await _userManager.AddClaimAsync(user, claim);

            if (result.Succeeded)
            {
                // Publier l'événement UserClaimChanged
                var changedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
                await _publisher.Publish(new UserClaimChangedEvent(
                    userId: user.Id,
                    email: user.Email!,
                    claimType: claimType,
                    claimValue: claimValue,
                    isAdded: true,
                    changedBy: changedBy
                ));
            }

            return result.Succeeded;
        }

        public async Task<bool> RemoveClaimAsync(string userId, string claimType, string claimValue)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var claim = new Claim(claimType, claimValue);
            var result = await _userManager.RemoveClaimAsync(user, claim);

            if (result.Succeeded)
            {
                // Publier l'événement UserClaimChanged
                var changedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
                await _publisher.Publish(new UserClaimChangedEvent(
                    userId: user.Id,
                    email: user.Email!,
                    claimType: claimType,
                    claimValue: claimValue,
                    isAdded: false,
                    changedBy: changedBy
                ));
            }

            return result.Succeeded;
        }

        public async Task<IList<Claim>> GetUserClaimsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<Claim>();

            return await _userManager.GetClaimsAsync(user);
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Vérifier les claims directs de l'utilisateur
            var userClaims = await _userManager.GetClaimsAsync(user);
            if (userClaims.Any(c => c.Type == AppClaims.Permission && c.Value == permission))
                return true;

            // Vérifier les claims des rôles
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    if (roleClaims.Any(c => c.Type == AppClaims.Permission && c.Value == permission))
                        return true;
                }
            }

            return false;
        }

        public async Task<IList<string>> GetAllRolesAsync()
        {
            return _roleManager.Roles.Select(r => r.Name!).ToList();
        }
    }
}

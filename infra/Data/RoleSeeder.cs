using domain.Constants;
using domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace infra.Data
{
    /// <summary>
    /// Seed initial des rôles et claims
    /// </summary>
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(
            RoleManager<IdentityRole> roleManager,
            ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Créer tous les rôles avec leurs claims
        /// </summary>
        public async Task SeedRolesAsync()
        {
            _logger.LogInformation("Starting role seeding...");

            // 1. Admin - Accès complet
            await CreateRoleWithClaimsAsync(AppRoles.Admin, new[]
            {
                // Forecasts
                new Claim(AppClaims.Permission, AppClaims.ForecastRead),
                new Claim(AppClaims.Permission, AppClaims.ForecastWrite),
                new Claim(AppClaims.Permission, AppClaims.ForecastDelete),
                
                // API Keys
                new Claim(AppClaims.Permission, AppClaims.ApiKeyManage),
                new Claim(AppClaims.Permission, AppClaims.ApiKeyViewAll),
                
                // Users
                new Claim(AppClaims.Permission, AppClaims.UserManage),
                new Claim(AppClaims.Permission, AppClaims.UserViewAll),
                
                // Access type
                new Claim(AppClaims.AccessType, AppClaims.WebAccess),
                new Claim(AppClaims.AccessType, AppClaims.ApiAccess)
            });

            // 2. User - Utilisateur standard (Web)
            await CreateRoleWithClaimsAsync(AppRoles.User, new[]
            {
                // Forecasts
                new Claim(AppClaims.Permission, AppClaims.ForecastRead),
                new Claim(AppClaims.Permission, AppClaims.ForecastWrite),
                new Claim(AppClaims.Permission, AppClaims.ForecastDelete),
                
                // API Keys (ses propres clés uniquement)
                new Claim(AppClaims.Permission, AppClaims.ApiKeyManage),
                
                // Access type
                new Claim(AppClaims.AccessType, AppClaims.WebAccess)
            });

            // 3. ApiConsumer - Consommateur API (via API Key)
            await CreateRoleWithClaimsAsync(AppRoles.ApiConsumer, new[]
            {
                // Forecasts (read + write)
                new Claim(AppClaims.Permission, AppClaims.ForecastRead),
                new Claim(AppClaims.Permission, AppClaims.ForecastWrite),
                
                // Access type
                new Claim(AppClaims.AccessType, AppClaims.ApiKeyAccess)
            });

            // 4. MobileUser - Utilisateur mobile (via JWT)
            await CreateRoleWithClaimsAsync(AppRoles.MobileUser, new[]
            {
                // Forecasts
                new Claim(AppClaims.Permission, AppClaims.ForecastRead),
                new Claim(AppClaims.Permission, AppClaims.ForecastWrite),
                
                // Access type
                new Claim(AppClaims.AccessType, AppClaims.ApiAccess)
            });

            _logger.LogInformation("Role seeding completed successfully");
        }

        /// <summary>
        /// Créer un rôle avec ses claims
        /// </summary>
        private async Task CreateRoleWithClaimsAsync(string roleName, Claim[] claims)
        {
            // Vérifier si le rôle existe déjà
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogInformation("Role {RoleName} already exists, updating claims...", roleName);

                var role = await _roleManager.FindByNameAsync(roleName);

                if (role != null)
                {
                    // Supprimer les anciens claims
                    var existingClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (var existingClaim in existingClaims)
                    {
                        await _roleManager.RemoveClaimAsync(role, existingClaim);
                    }

                    // Ajouter les nouveaux claims
                    foreach (var claim in claims)
                    {
                        await _roleManager.AddClaimAsync(role, claim);
                    }

                    _logger.LogInformation("Role {RoleName} claims updated", roleName);
                }
                return;
            }

            // Créer le rôle
            var newRole = new IdentityRole(roleName);
            var result = await _roleManager.CreateAsync(newRole);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create role {RoleName}: {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Role {RoleName} created successfully", roleName);

            // Ajouter les claims au rôle
            foreach (var claim in claims)
            {
                var claimResult = await _roleManager.AddClaimAsync(newRole, claim);
                if (!claimResult.Succeeded)
                {
                    _logger.LogError("Failed to add claim {ClaimType}:{ClaimValue} to role {RoleName}",
                        claim.Type, claim.Value, roleName);
                }
            }

            _logger.LogInformation("Claims added to role {RoleName}", roleName);
        }

        /// <summary>
        /// Créer un utilisateur admin par défaut
        /// </summary>
        public async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "admin@weatherforecast.com";
            const string adminPassword = "Admin@123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                _logger.LogInformation("Creating default admin user...");

                adminUser = new ApplicationUser(adminEmail, "Admin", "User");

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                    _logger.LogInformation("Admin user created: {Email}", adminEmail);
                }
                else
                {
                    _logger.LogError("Failed to create admin user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Admin user already exists");

                // S'assurer qu'il a le rôle Admin
                if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
                {
                    await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                    _logger.LogInformation("Admin role added to existing user");
                }
            }
        }
    }
}

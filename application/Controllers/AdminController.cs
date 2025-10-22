using application.Authorization;
using application.ViewModels.Admin;
using domain.Constants;
using domain.Entities;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace application.Controllers
{
    /// <summary>
    /// Panel d'administration - Gestion des utilisateurs
    /// Accessible uniquement aux Admins
    /// </summary>
    [Authorize]
    [HasPermission(AppClaims.UserManage)]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserManagementService _userManagementService;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserManagementService userManagementService,
            ISessionManagementService sessionManagementService,
            IApiKeyService apiKeyService,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userManagementService = userManagementService;
            _sessionManagementService = sessionManagementService;
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        // GET: /Admin
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userViewModels = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);

                if (!roles.Contains("Admin"))
                {
                    userViewModels.Add(new UserListViewModel
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        Roles = roles.ToList(),
                        ClaimsCount = claims.Count
                    });
                }

            }

            return View(userViewModels.OrderByDescending(u => u.CreatedAt));
        }

        // GET: /Admin/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(user.Id);
            var sessions = await _sessionManagementService.GetActiveSessionsAsync(user.Id);

            var viewModel = new UserDetailsViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList(),
                Claims = claims.Select(c => new ClaimViewModel
                {
                    Type = c.Type,
                    Value = c.Value
                }).ToList(),
                ApiKeys = apiKeys.Select(k => new ApiKeyViewModel
                {
                    Id = k.Id,
                    Key = k.Key,
                    Name = k.Name,
                    Scopes = k.Scopes.ToScopeString(),
                    IsActive = k.IsActive,
                    IsRevoked = k.IsRevoked,
                    ExpiresAt = k.ExpiresAt,
                    LastUsedAt = k.LastUsedAt,
                    RequestCount = k.RequestCount,
                    CreatedAt = k.CreatedAt
                }).ToList(),
                Sessions = sessions.Select(s => new SessionViewModel
                {
                    Id = s.Id,
                    Type = s.Type.ToString(),
                    IpAddress = s.IpAddress,
                    UserAgent = s.UserAgent,
                    IsRevoked = s.IsRevoked,
                    ExpiresAt = s.ExpiresAt,
                    CreatedAt = s.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: /Admin/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateUserViewModel
            {
                AvailableRoles = AppRoles.All.ToList(),
                SelectedRoles = new List<string>()
            };

            return View(viewModel);
        }

        // POST: /Admin/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var (success, errors, user) = await _userManagementService.RegisterAsync(
                    model.Email,
                    model.Password,
                    model.FirstName,
                    model.LastName);

                if (success && user != null)
                {
                    // Assigner les rôles sélectionnés
                    foreach (var role in model.SelectedRoles)
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }

                    // Assigner les claims personnalisés si fournis
                    if (!string.IsNullOrWhiteSpace(model.CustomClaimType) &&
                        !string.IsNullOrWhiteSpace(model.CustomClaimValue))
                    {
                        await _userManager.AddClaimAsync(user,
                            new System.Security.Claims.Claim(model.CustomClaimType, model.CustomClaimValue));
                    }

                    _logger.LogInformation("Admin created user: {Email} with roles: {Roles}",
                        user.Email, string.Join(", ", model.SelectedRoles));

                    TempData["SuccessMessage"] = $"Utilisateur {user.Email} créé avec succès.";
                    return RedirectToAction(nameof(Details), new { id = user.Id });
                }

                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            model.AvailableRoles = AppRoles.All.ToList();
            return View(model);
        }

        // GET: /Admin/EditRoles/5
        [HttpGet("EditRoles/{id}")]
        public async Task<IActionResult> EditRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);

            var viewModel = new EditRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                AvailableRoles = AppRoles.All.ToList(),
                SelectedRoles = userRoles.ToList(),
                Claims = userClaims.Select(c => new ClaimViewModel
                {
                    Type = c.Type,
                    Value = c.Value
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: /Admin/EditRoles/5
        [HttpPost("EditRoles/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(string id, EditRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Supprimer tous les rôles existants
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Ajouter les nouveaux rôles
            if (model.SelectedRoles != null && model.SelectedRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, model.SelectedRoles);
            }

            _logger.LogInformation("Admin updated roles for user {Email}: {Roles}",
                user.Email, string.Join(", ", model.SelectedRoles ?? new List<string>()));

            TempData["SuccessMessage"] = "Rôles mis à jour avec succès.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/AddClaim/5
        [HttpPost("AddClaim/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddClaim(string id, string claimType, string claimValue)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(claimType) || string.IsNullOrWhiteSpace(claimValue))
            {
                TempData["ErrorMessage"] = "Le type et la valeur du claim sont requis.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(claimType, claimValue));

            _logger.LogInformation("Admin added claim {Type}:{Value} to user {Email}",
                claimType, claimValue, user.Email);

            TempData["SuccessMessage"] = "Claim ajouté avec succès.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/RemoveClaim/5
        [HttpPost("RemoveClaim/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveClaim(string id, string claimType, string claimValue)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.RemoveClaimAsync(user, new System.Security.Claims.Claim(claimType, claimValue));

            _logger.LogInformation("Admin removed claim {Type}:{Value} from user {Email}",
                claimType, claimValue, user.Email);

            TempData["SuccessMessage"] = "Claim supprimé avec succès.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/ToggleActive/5
        [HttpPost("ToggleActive/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.IsActive)
            {
                user.Deactivate();
                TempData["SuccessMessage"] = "Utilisateur désactivé.";
            }
            else
            {
                user.Activate();
                TempData["SuccessMessage"] = "Utilisateur activé.";
            }

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Admin toggled active status for user {Email}: {IsActive}",
                user.Email, user.IsActive);

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/RevokeSession/5
        [HttpPost("RevokeSession/{sessionId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSession(Guid sessionId, string userId)
        {
            await _sessionManagementService.RevokeAsync(sessionId);

            _logger.LogInformation("Admin revoked session {SessionId}", sessionId);

            TempData["SuccessMessage"] = "Session révoquée avec succès.";
            return RedirectToAction(nameof(Details), new { id = userId });
        }
    }
}

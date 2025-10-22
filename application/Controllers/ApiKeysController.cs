using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using domain.Entities;
using domain.Interfaces.Services;
using domain.Constants;
using application.Authorization;

namespace application.Controllers
{
    [Authorize]
    public class ApiKeysController : Controller
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApiKeysController> _logger;

        public ApiKeysController(
            IApiKeyService apiKeyService,
            UserManager<ApplicationUser> userManager,
            ILogger<ApiKeysController> logger)
        {
            _apiKeyService = apiKeyService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /ApiKeys
        [HasPermission(AppClaims.ApiKeyManage)]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(user.Id);
            return View(apiKeys);
        }

        // GET: /ApiKeys/Create
        [HasPermission(AppClaims.ApiKeyManage)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /ApiKeys/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(AppClaims.ApiKeyManage)]
        public async Task<IActionResult> Create(string name, int? expirationDays)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("name", "Le nom est requis");
                return View();
            }

            try
            {
                var (apiKey, plainSecret) = await _apiKeyService.GenerateApiKeyAsync(
                    user.Id, 
                    name, 
                    expirationDays);

                _logger.LogInformation("Clé API créée pour {Email}: {Key}", user.Email, apiKey.Key);

                // Stocker temporairement le secret pour l'afficher (une seule fois)
                TempData["NewApiKey"] = apiKey.Key;
                TempData["NewApiSecret"] = plainSecret;
                TempData["SuccessMessage"] = "Clé API créée avec succès ! Copiez le secret maintenant, il ne sera plus affiché.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la clé API");
                ModelState.AddModelError(string.Empty, "Erreur lors de la création de la clé API");
                return View();
            }
        }

        // GET: /ApiKeys/Revoke/5
        [HasPermission(AppClaims.ApiKeyManage)]
        public async Task<IActionResult> Revoke(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var apiKey = (await _apiKeyService.GetUserApiKeysAsync(user.Id))
                .FirstOrDefault(k => k.Id == id);

            if (apiKey == null)
            {
                return NotFound();
            }

            return View(apiKey);
        }

        // POST: /ApiKeys/Revoke/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(AppClaims.ApiKeyManage)]
        public async Task<IActionResult> RevokeConfirmed(int id, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                // Raison par défaut si non spécifiée
                var revocationReason = string.IsNullOrWhiteSpace(reason)
                    ? $"Révoquée par l'utilisateur {user.Email} depuis l'interface web"
                    : $"{reason} (par {user.Email})";

                var success = await _apiKeyService.RevokeApiKeyAsync(id, user.Id, revocationReason);
                
                if (success)
                {
                    _logger.LogInformation("Clé API révoquée: {Id} par {Email}. Raison: {Reason}", 
                        id, user.Email, revocationReason);
                    TempData["SuccessMessage"] = "Clé API révoquée avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = "Clé API introuvable ou déjà révoquée";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la révocation de la clé API {Id}", id);
                TempData["ErrorMessage"] = "Erreur lors de la révocation de la clé API";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

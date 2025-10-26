using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using domain.Entities;
using domain.Interfaces.Services;
using domain.Constants;
using application.Authorization;
using application.Validators;
using domain.Events;
using domain.Exceptions;
using application.Helpers;

namespace application.Controllers
{
    [Authorize]
    public class ApiKeysController : Controller
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPublisher _publisher;
        private readonly ILogger<ApiKeysController> _logger;

        public ApiKeysController(
            IApiKeyService apiKeyService,
            UserManager<ApplicationUser> userManager,
            IPublisher publisher,
            ILogger<ApiKeysController> logger)
        {
            _apiKeyService = apiKeyService;
            _userManager = userManager;
            _publisher = publisher;
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
        public async Task<IActionResult> Create(CreateApiKeyRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // ✅ Validation FluentValidation via ModelState
            if (!ModelState.IsValid)
            {
                // Publier l'erreur pour notification SignalR
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                await _publisher.PublishValidationErrorAsync(
                    User,
                    errors,
                    "Create",
                    "ApiKey",
                    null,
                    null);

                return View();
            }

            try
            {
                var (apiKey, plainSecret) = await _apiKeyService.GenerateApiKeyAsync(
                    user.Id, 
                    request.Name, 
                    request.ExpirationDays);

                _logger.LogInformation("Clé API créée pour {Email}: {Key}", user.Email, apiKey.Key);

                // Stocker temporairement le secret pour l'afficher (une seule fois)
                TempData["NewApiKey"] = apiKey.Key;
                TempData["NewApiSecret"] = plainSecret;
                TempData["SuccessMessage"] = "Clé API créée avec succès ! Copiez le secret maintenant, il ne sera plus affiché.";

                return RedirectToAction(nameof(Index));
            }
            catch (DomainException ex)
            {
                // ✅ Autre exception domain (Database, etc.)
                _logger.LogError(ex, "Erreur domain lors de la création de la clé API");
                TempData["ErrorMessage"] = ex.Message;
                
                await _publisher.PublishDomainExceptionAsync(User, ex);
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la création de la clé API");
                var errorMessage = "Une erreur inattendue est survenue.";
                TempData["ErrorMessage"] = errorMessage;
                
                // Publier l'erreur pour notification temps réel
                await _publisher.PublishGenericErrorAsync(
                    User,
                    errorMessage,
                    "Create",
                    "ApiKey",
                    null,
                    ex);
                
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
            }
            catch (DomainException ex)
            {
                // ✅ Exception typée du domain
                _logger.LogError(ex, "Erreur domain lors de la révocation de la clé API {Id}", id);
                TempData["ErrorMessage"] = ex.Message;
                
                await _publisher.PublishDomainExceptionAsync(User, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la révocation de la clé API {Id}", id);
                var errorMessage = "Erreur lors de la révocation de la clé API";
                TempData["ErrorMessage"] = errorMessage;
                
                // Publier l'erreur pour notification temps réel (bufferisée car redirect)
                await _publisher.PublishGenericErrorAsync(
                    User,
                    errorMessage,
                    "Revoke",
                    "ApiKey",
                    id.ToString(),
                    ex);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

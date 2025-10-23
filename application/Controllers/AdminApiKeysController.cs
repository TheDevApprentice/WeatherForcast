using application.Authorization;
using domain.Constants;
using domain.Entities;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace application.Controllers
{
    /// <summary>
    /// Contrôleur Admin pour gérer TOUTES les clés API
    /// Nécessite la permission ApiKeyViewAll (Admin uniquement)
    /// </summary>
    [Authorize]
    [HasPermission(AppClaims.ApiKeyViewAll)]
    [Route("Admin/ApiKeys")]
    public class AdminApiKeysController : Controller
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminApiKeysController> _logger;

        public AdminApiKeysController(
            IApiKeyService apiKeyService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminApiKeysController> logger)
        {
            _apiKeyService = apiKeyService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Admin/ApiKeys/RevokeAny/5
        [HttpGet("RevokeAny/{id}")]
        public async Task<IActionResult> RevokeAny(int id)
        {
            // TODO: Vérifier le rôle Admin
            // if (!User.IsInRole("Admin"))
            //     return Forbid();

            // Pour l'instant, récupérer via le premier utilisateur trouvé
            // En production, il faudra un repository qui récupère toutes les clés

            TempData["InfoMessage"] = "⚠️ Fonctionnalité Admin en cours de développement. Les rôles ne sont pas encore implémentés.";
            return RedirectToAction("Index", "ApiKeys");
        }

        // POST: /Admin/ApiKeys/RevokeAny/5
        [HttpPost("RevokeAny/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAnyConfirmed(int id, string reason)
        {
            // TODO: Vérifier le rôle Admin
            // if (!User.IsInRole("Admin"))
            //     return Forbid();

            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "La raison est obligatoire pour une révocation admin";
                return RedirectToAction(nameof(RevokeAny), new { id });
            }

            try
            {
                var revocationReason = $"[ADMIN] {reason} (par {admin.Email})";

                // TODO: Implémenter une méthode RevokeAnyApiKeyAsync qui ne vérifie pas le userId
                // var success = await _apiKeyService.RevokeAnyApiKeyAsync(id, revocationReason);

                _logger.LogWarning("Tentative de révocation admin de la clé {Id} par {Email}. Raison: {Reason}",
                    id, admin.Email, reason);

                TempData["InfoMessage"] = "⚠️ Fonctionnalité Admin en cours de développement";
                return RedirectToAction("Index", "ApiKeys");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la révocation admin de la clé API {Id}", id);
                TempData["ErrorMessage"] = "Erreur lors de la révocation de la clé API";
                return RedirectToAction("Index", "ApiKeys");
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using domain.Entities;
using application.ViewModels;
using domain.Interfaces.Services;

namespace application.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IRateLimitService _rateLimitService;

        public AuthController(
            IUserManagementService userManagementService,
            ISessionManagementService sessionManagementService,
            SignInManager<ApplicationUser> signInManager,
            IRateLimitService rateLimitService)
        {
            _userManagementService = userManagementService;
            _sessionManagementService = sessionManagementService;
            _signInManager = signInManager;
            _rateLimitService = rateLimitService;
        }

        // GET: /Auth/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
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
                    // Ne pas auto-login après registration
                    // Rediriger vers la page de login
                    TempData["SuccessMessage"] = "Compte créé avec succès. Veuillez vous connecter.";
                    return RedirectToAction("Login");
                }

                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            return View(model);
        }

        // GET: /Auth/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Récupérer l'utilisateur
                    var user = await _userManagementService.GetByEmailAsync(model.Email);
                    if (user != null)
                    {
                        // Récupérer les informations de la requête
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                        
                        // Réinitialiser les tentatives échouées (login réussi)
                        if (!string.IsNullOrEmpty(ipAddress))
                        {
                            await _rateLimitService.ResetFailedAttemptsAsync(ipAddress);
                        }
                        
                        // Utiliser l'ID utilisateur comme token de session
                        // (Le cookie Identity sera créé automatiquement par SignInManager)
                        var sessionToken = user.Id;
                        
                        // Révoquer les anciennes sessions Web
                        await _sessionManagementService.RevokeAllByUserIdAsync(user.Id);
                        
                        // Créer la nouvelle session Web
                        await _sessionManagementService.CreateWebSessionAsync(
                            user.Id,
                            sessionToken,
                            ipAddress,
                            userAgent,
                            expirationDays: model.RememberMe ? 30 : 7);
                        
                        // Mettre à jour LastLoginAt
                        await _userManagementService.UpdateLastLoginAsync(user.Id);
                    }
                    
                    return RedirectToLocal(returnUrl);
                }

                // Enregistrer les tentatives échouées (brute force protection)
                var failedIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(failedIpAddress))
                {
                    await _rateLimitService.RecordFailedLoginAttemptAsync(failedIpAddress, model.Email);
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Compte verrouillé. Réessayez dans 5 minutes.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
                }
            }

            return View(model);
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // 1. Récupérer l'ID de l'utilisateur avant de le déconnecter
            var userId = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            // 2. Supprimer uniquement la session Web (Type = 1) sans toucher aux sessions API
            if (!string.IsNullOrEmpty(userId))
            {
                // Récupérer toutes les sessions actives
                var activeSessions = await _sessionManagementService.GetActiveSessionsAsync(userId);
                
                // Filtrer et supprimer uniquement les sessions Web (Type = 1)
                // La suppression cascade supprimera aussi les UserSessions associées
                var webSessions = activeSessions.Where(s => s.Type == domain.Entities.SessionType.Web);
                foreach (var session in webSessions)
                {
                    await _sessionManagementService.DeleteAsync(session.Id);
                }
            }

            // 3. Déconnecter l'utilisateur (supprime le cookie Identity)
            await _signInManager.SignOutAsync();
            
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}

using api.DTOs;
using domain.Constants;
using domain.Entities;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IJwtService _jwtService;
        private readonly IRateLimitService _rateLimitService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserManagementService userManagementService,
            ISessionManagementService sessionManagementService,
            IAuthenticationService authenticationService,
            IJwtService jwtService,
            IRateLimitService rateLimitService,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthController> logger)
        {
            _userManagementService = userManagementService;
            _sessionManagementService = sessionManagementService;
            _authenticationService = authenticationService;
            _jwtService = jwtService;
            _rateLimitService = rateLimitService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Inscription d'un nouvel utilisateur mobile
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Enregistrer l'utilisateur
            var (success, errors, user) = await _userManagementService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            if (!success || user == null)
            {
                return BadRequest(new { Errors = errors });
            }

            // Assigner le rôle MobileUser
            await _userManager.AddToRoleAsync(user, AppRoles.MobileUser);

            _logger.LogInformation("Nouvel utilisateur mobile enregistré: {Email}", user.Email);

            // Retourner seulement les infos utilisateur, pas de token
            // L'utilisateur doit se connecter via /api/auth/login pour obtenir un token
            return Ok(new
            {
                Message = "Compte créé avec succès. Veuillez vous connecter.",
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }

        /// <summary>
        /// Connexion d'un utilisateur mobile existant
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Récupérer les informations de la requête
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Valider les credentials
            var (success, user) = await _authenticationService.ValidateCredentialsAsync(
                request.Email,
                request.Password);

            if (!success || user == null)
            {
                // Enregistrer la tentative échouée (brute force protection)
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    await _rateLimitService.RecordFailedLoginAttemptAsync(ipAddress, request.Email);
                }

                return Unauthorized(new { Message = "Email ou mot de passe incorrect" });
            }

            // Réinitialiser les tentatives échouées (login réussi)
            if (!string.IsNullOrEmpty(ipAddress))
            {
                await _rateLimitService.ResetFailedAttemptsAsync(ipAddress);
            }

            // Mettre à jour LastLoginAt
            await _userManagementService.UpdateLastLoginAsync(user.Id);

            // Générer le token JWT (avec rôles et claims)
            var token = await _jwtService.GenerateTokenAsync(user);

            // Créer une session API
            await _sessionManagementService.CreateApiSessionAsync(
                user.Id,
                token,
                ipAddress,
                userAgent,
                expirationHours: 24);

            var response = new AuthResponse
            {
                Token = token,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _logger.LogInformation("Utilisateur mobile connecté: {Email}", user.Email);

            return Ok(response);
        }

        /// <summary>
        /// Endpoint protégé pour tester l'authentification
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCurrentUser()
        {
            // 1. Vérifier que le claim email existe dans le JWT
            var email = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Tentative d'accès à /me sans claim email");
                return Unauthorized(new { message = "Token invalide : email manquant" });
            }

            // 2. Vérifier que l'utilisateur existe
            var user = await _userManagementService.GetByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("Utilisateur {Email} introuvable", email);
                return Unauthorized(new { message = "Utilisateur introuvable" });
            }

            // 3. Vérifier que l'utilisateur a au moins une session API active et valide
            var activeSessions = await _sessionManagementService.GetActiveSessionsAsync(user.Id);
            var hasValidApiSession = activeSessions.Any(s =>
                s.Type == domain.Entities.SessionType.Api &&
                s.ExpiresAt > DateTime.UtcNow &&
                !s.IsRevoked);

            if (!hasValidApiSession)
            {
                _logger.LogWarning("Aucune session API valide pour l'utilisateur {Email}", email);
                return Unauthorized(new { message = "Session expirée ou invalide" });
            }

            _logger.LogInformation("Accès autorisé à /me pour {Email}", email);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.CreatedAt
            });
        }

        /// <summary>
        /// Déconnexion d'un utilisateur mobile (révoque la session API)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Récupérer l'ID de l'utilisateur depuis le token JWT
                var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Récupérer le token JWT depuis l'en-tête Authorization
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                var token = authHeader.Replace("Bearer ", "");

                // Révoquer uniquement la session API correspondant à ce token
                var activeSessions = await _sessionManagementService.GetActiveSessionsAsync(userId);
                var currentSession = activeSessions.FirstOrDefault(s => s.Token == token && s.Type == domain.Entities.SessionType.Api);

                if (currentSession != null)
                {
                    await _sessionManagementService.RevokeAsync(currentSession.Id, "Déconnexion utilisateur", userId);
                }

                _logger.LogInformation("Utilisateur mobile déconnecté: {UserId}", userId);

                return Ok(new { Message = "Déconnexion réussie" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion");
                return StatusCode(500, new { Message = "Erreur lors de la déconnexion" });
            }
        }
    }
}

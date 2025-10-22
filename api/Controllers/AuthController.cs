using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using api.DTOs;
using domain.Interfaces.Services;
using domain.Constants;
using domain.Entities;

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

            // Générer le token JWT (avec rôles et claims)
            var token = await _jwtService.GenerateTokenAsync(user);

            // Récupérer les informations de la requête
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

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

            _logger.LogInformation("Nouvel utilisateur mobile enregistré: {Email}", user.Email);

            return Ok(response);
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
        [ProducesResponseType(typeof(api.DTOs.ErrorResponse), 401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized();
            }

            var user = await _userManagementService.GetByEmailAsync(email);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.CreatedAt
            });
        }
    }
}

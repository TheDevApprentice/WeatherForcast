using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.DTOs;
using domain.Interfaces.Services;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IJwtService jwtService,
            IRateLimitService rateLimitService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _jwtService = jwtService;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        // NOTE: L'inscription se fait via l'application Web, pas via l'API
        // Pour utiliser l'API, créez un compte sur l'application Web puis générez une clé API
        
        /// <summary>
        /// Inscription d'un nouvel utilisateur (DÉSACTIVÉ - Utiliser l'application Web)
        /// </summary>
        /* [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Générer le token JWT en avance
            var tempUser = new domain.Entities.ApplicationUser { Email = request.Email };
            var token = _jwtService.GenerateToken(tempUser);

            // Récupérer les informations de la requête
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Enregistrer l'utilisateur ET créer sa session (opération atomique)
            var (success, errors, user) = await _authService.RegisterWithSessionAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                token,
                ipAddress,
                userAgent,
                isApiSession: true,
                expirationHours: 24);

            if (!success || user == null)
            {
                return BadRequest(new { Errors = errors });
            }

            // Regénérer le token avec le vrai user (pour avoir le bon ID)
            token = _jwtService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _logger.LogInformation("Nouvel utilisateur enregistré: {Email}", user.Email);

            return Ok(response);
        } */

        /// <summary>
        /// Connexion d'un utilisateur existant (DÉSACTIVÉ - Utiliser les clés API)
        /// </summary>
        /* [HttpPost("login")]
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

            // Pré-générer le token (on le regénérera avec le vrai user après)
            var tempUser = new domain.Entities.ApplicationUser { Email = request.Email };
            var token = _jwtService.GenerateToken(tempUser);

            // Login + créer session + mettre à jour LastLoginAt (opération atomique)
            var (success, user) = await _authService.LoginWithSessionAsync(
                request.Email,
                request.Password,
                token,
                ipAddress,
                userAgent,
                isApiSession: true,
                expirationHours: 24);

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

            // Regénérer le token avec le vrai user (pour avoir le bon ID)
            token = _jwtService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _logger.LogInformation("Utilisateur connecté: {Email}", user.Email);

            return Ok(response);
        } */

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

            var user = await _authService.GetUserByEmailAsync(email);

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

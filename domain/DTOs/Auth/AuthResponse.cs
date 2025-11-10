namespace domain.DTOs.Auth
{
    /// <summary>
    /// Réponse d'authentification et informations utilisateur
    /// Utilisé pour /login (avec Token) et /me (sans Token)
    /// </summary>
    /// <remarks>
    /// Endpoints:
    /// - POST /api/auth/login : Retourne Token et ExpiresAt
    /// - GET /api/auth/me : Retourne uniquement les infos utilisateur (Token et ExpiresAt sont null)
    /// 
    /// Sécurité:
    /// - L'ID utilisateur n'est JAMAIS renvoyé (il est dans le token JWT)
    /// - Le token contient les claims : NameIdentifier, Email, GivenName, Surname, Roles
    /// </remarks>
    public class AuthResponse
    {
        /// <summary>
        /// Token JWT (présent uniquement lors du login)
        /// </summary>
        /// <remarks>
        /// - Null pour l'endpoint /me
        /// - Contient l'ID utilisateur dans le claim NameIdentifier
        /// - Durée de validité : 24 heures
        /// - Algorithme : HS256
        /// </remarks>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string? Token { get; set; }
        
        /// <summary>
        /// Email de l'utilisateur
        /// </summary>
        /// <example>user@example.com</example>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Prénom de l'utilisateur
        /// </summary>
        /// <example>John</example>
        public string FirstName { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom de famille de l'utilisateur
        /// </summary>
        /// <example>Doe</example>
        public string LastName { get; set; } = string.Empty;
        
        /// <summary>
        /// Date d'expiration du token (présent uniquement lors du login)
        /// </summary>
        /// <remarks>
        /// - Null pour l'endpoint /me
        /// - Format UTC
        /// - Durée : 24 heures après la connexion
        /// </remarks>
        /// <example>2025-11-11T15:30:00Z</example>
        public DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// Date de création du compte utilisateur
        /// </summary>
        /// <remarks>Format UTC</remarks>
        /// <example>2024-01-15T10:00:00Z</example>
        public DateTime CreatedAt { get; set; }
    }
}

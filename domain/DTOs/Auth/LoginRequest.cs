namespace domain.DTOs.Auth
{
    /// <summary>
    /// DTO pour la connexion d'un utilisateur
    /// Validation déléguée à FluentValidation (LoginRequestValidator)
    /// Utilisé par API et Mobile
    /// </summary>
    /// <remarks>
    /// Endpoint: POST /api/auth/login
    /// Authentification: Aucune (AllowAnonymous)
    /// </remarks>
    public class LoginRequest
    {
        /// <summary>
        /// Adresse email de l'utilisateur
        /// </summary>
        /// <example>user@example.com</example>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Mot de passe de l'utilisateur
        /// </summary>
        /// <remarks>
        /// Le mot de passe doit respecter les règles de sécurité :
        /// - Minimum 8 caractères
        /// - Au moins une majuscule
        /// - Au moins une minuscule
        /// - Au moins un chiffre
        /// - Au moins un caractère spécial
        /// </remarks>
        public string Password { get; set; } = string.Empty;
    }
}

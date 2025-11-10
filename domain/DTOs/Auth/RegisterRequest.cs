namespace domain.DTOs.Auth
{
    /// <summary>
    /// DTO pour l'inscription d'un nouvel utilisateur
    /// Validation déléguée à FluentValidation (RegisterRequestValidator)
    /// Utilisé par API et Mobile
    /// </summary>
    /// <remarks>
    /// Endpoint: POST /api/auth/register
    /// Authentification: Aucune (AllowAnonymous)
    /// Rôle assigné automatiquement: MobileUser
    /// </remarks>
    public class RegisterRequest
    {
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
        /// Adresse email de l'utilisateur (doit être unique)
        /// </summary>
        /// <example>john.doe@example.com</example>
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
        /// <example>P@ssw0rd123</example>
        public string Password { get; set; } = string.Empty;
    }
}

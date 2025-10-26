namespace api.DTOs
{
    /// <summary>
    /// DTO pour la connexion d'un utilisateur mobile (API)
    /// Validation déléguée à FluentValidation (LoginRequestValidator)
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}

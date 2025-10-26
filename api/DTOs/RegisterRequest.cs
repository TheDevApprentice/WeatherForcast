namespace api.DTOs
{
    /// <summary>
    /// DTO pour l'inscription d'un utilisateur mobile (API)
    /// Validation déléguée à FluentValidation (RegisterRequestValidator)
    /// </summary>
    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}

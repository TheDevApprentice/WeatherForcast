namespace mobile.Models.DTOs
{
    /// <summary>
    /// Requête de connexion
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

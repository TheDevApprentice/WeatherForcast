namespace mobile.Models.DTOs
{
    /// <summary>
    /// Réponse d'authentification de l'API
    /// </summary>
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}

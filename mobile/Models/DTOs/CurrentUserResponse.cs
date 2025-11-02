namespace mobile.Models.DTOs
{
    /// <summary>
    /// Réponse de l'API /me contenant les informations de l'utilisateur connecté
    /// </summary>
    public class CurrentUserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

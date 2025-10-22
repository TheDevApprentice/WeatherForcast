namespace domain.Entities
{
    /// <summary>
    /// Table de liaison User ↔ Session
    /// Un utilisateur peut avoir plusieurs sessions actives
    /// </summary>
    public class UserSession
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// ID de l'utilisateur
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de la session
        /// </summary>
        public Guid SessionId { get; set; }
        
        /// <summary>
        /// Date de création de la liaison
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Session Session { get; set; } = null!;
    }
}

namespace domain.Entities
{
    /// <summary>
    /// Table de liaison User ↔ Session
    /// Un utilisateur peut avoir plusieurs sessions actives
    /// </summary>
    public class UserSession
    {
        public Guid Id { get; internal set; }
        
        /// <summary>
        /// ID de l'utilisateur
        /// </summary>
        public string UserId { get; private set; } = string.Empty;
        
        /// <summary>
        /// ID de la session
        /// </summary>
        public Guid SessionId { get; private set; }
        
        /// <summary>
        /// Date de création de la liaison
        /// </summary>
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Session Session { get; set; } = null!;

        /// <summary>
        /// Constructeur parameterless pour EF Core
        /// </summary>
        private UserSession()
        {
        }

        /// <summary>
        /// Constructeur pour créer une nouvelle liaison User-Session
        /// </summary>
        public UserSession(string userId, Guid sessionId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("L'ID utilisateur est requis", nameof(userId));

            if (sessionId == Guid.Empty)
                throw new ArgumentException("L'ID session est requis", nameof(sessionId));

            Id = Guid.NewGuid();
            UserId = userId;
            SessionId = sessionId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}

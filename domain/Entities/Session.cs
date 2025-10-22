namespace domain.Entities
{
    /// <summary>
    /// Session utilisateur (pour Web cookies et API JWT)
    /// Permet de révoquer les sessions
    /// </summary>
    public class Session
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// Token (Cookie ID pour Web ou JWT pour API)
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// Type de session (Web, API)
        /// </summary>
        public SessionType Type { get; set; }
        
        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date d'expiration
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Session révoquée ?
        /// </summary>
        public bool IsRevoked { get; set; } = false;
        
        /// <summary>
        /// Date de révocation (si révoquée)
        /// </summary>
        public DateTime? RevokedAt { get; set; }
        
        /// <summary>
        /// IP de la session
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// User Agent (navigateur/app)
        /// </summary>
        public string? UserAgent { get; set; }
        
        // Navigation property
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    }
    
    public enum SessionType
    {
        Web = 1,    // Cookie-based (MVC)
        Api = 2     // JWT-based (REST API)
    }
}

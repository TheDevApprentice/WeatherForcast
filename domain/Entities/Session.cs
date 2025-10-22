namespace domain.Entities
{
    /// <summary>
    /// Session utilisateur (pour Web cookies et API JWT)
    /// Entité riche avec encapsulation et logique métier
    /// </summary>
    public class Session
    {
        public Guid Id { get; private set; }
        
        /// <summary>
        /// Token (Cookie ID pour Web ou JWT pour API)
        /// </summary>
        public string Token { get; private set; } = string.Empty;
        
        /// <summary>
        /// Type de session (Web, API)
        /// </summary>
        public SessionType Type { get; private set; }
        
        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date d'expiration
        /// </summary>
        public DateTime ExpiresAt { get; private set; }
        
        /// <summary>
        /// Session révoquée ?
        /// </summary>
        public bool IsRevoked { get; private set; } = false;
        
        /// <summary>
        /// Date de révocation (si révoquée)
        /// </summary>
        public DateTime? RevokedAt { get; private set; }
        
        /// <summary>
        /// Raison de la révocation
        /// </summary>
        public string? RevocationReason { get; private set; }
        
        /// <summary>
        /// IP de la session
        /// </summary>
        public string? IpAddress { get; private set; }
        
        /// <summary>
        /// User Agent (navigateur/app)
        /// </summary>
        public string? UserAgent { get; private set; }
        
        // Navigation property
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

        /// <summary>
        /// Constructeur parameterless pour EF Core
        /// </summary>
        private Session()
        {
        }

        /// <summary>
        /// Constructeur pour créer une nouvelle session
        /// </summary>
        public Session(string token, SessionType type, DateTime expiresAt, string? ipAddress = null, string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Le token est requis", nameof(token));

            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("La date d'expiration doit être dans le futur", nameof(expiresAt));

            Id = Guid.NewGuid();
            Token = token;
            Type = type;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            IsRevoked = false;
        }

        /// <summary>
        /// Révoquer la session
        /// </summary>
        public void Revoke(string? reason = null)
        {
            if (IsRevoked)
                throw new InvalidOperationException("La session est déjà révoquée");

            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevocationReason = reason;
        }

        /// <summary>
        /// Vérifier si la session est expirée
        /// </summary>
        public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// Vérifier si la session est valide (non révoquée et non expirée)
        /// </summary>
        public bool IsValid() => !IsRevoked && !IsExpired();

        /// <summary>
        /// Prolonger la session
        /// </summary>
        public void Extend(TimeSpan duration)
        {
            if (IsRevoked)
                throw new InvalidOperationException("Impossible de prolonger une session révoquée");

            if (IsExpired())
                throw new InvalidOperationException("Impossible de prolonger une session expirée");

            ExpiresAt = DateTime.UtcNow.Add(duration);
        }

        /// <summary>
        /// Obtenir la durée de vie restante
        /// </summary>
        public TimeSpan? GetRemainingLifetime()
        {
            if (IsRevoked || IsExpired())
                return null;

            return ExpiresAt - DateTime.UtcNow;
        }

        /// <summary>
        /// Vérifier si la session est une session Web
        /// </summary>
        public bool IsWebSession() => Type == SessionType.Web;

        /// <summary>
        /// Vérifier si la session est une session API
        /// </summary>
        public bool IsApiSession() => Type == SessionType.Api;
    }
    
    public enum SessionType
    {
        Web = 1,    // Cookie-based (MVC)
        Api = 2     // JWT-based (REST API)
    }
}

namespace domain.Entities
{
    /// <summary>
    /// Clé API pour accéder à l'API REST
    /// Utilise le standard OAuth2 (Client Credentials)
    /// </summary>
    public class ApiKey
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Nom de la clé (ex: "Mon Application Mobile")
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// La clé API (client_id)
        /// Format: wf_live_xxxxxxxxxxxx (32 caractères aléatoires)
        /// </summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// Le secret de la clé (client_secret) - Hashé en base
        /// Affiché UNE SEULE FOIS à la création
        /// </summary>
        public string SecretHash { get; set; } = string.Empty;
        
        /// <summary>
        /// Utilisateur propriétaire de la clé
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière utilisation
        /// </summary>
        public DateTime? LastUsedAt { get; set; }
        
        /// <summary>
        /// Date d'expiration (optionnel)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// La clé est-elle active ?
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Scopes autorisés (ex: "read", "read write")
        /// </summary>
        public string Scopes { get; set; } = "read";
        
        /// <summary>
        /// Nombre total de requêtes effectuées avec cette clé
        /// </summary>
        public long RequestCount { get; set; } = 0;
        
        /// <summary>
        /// Adresse IP autorisée (optionnel, pour restreindre l'usage)
        /// </summary>
        public string? AllowedIpAddress { get; set; }
    }
}

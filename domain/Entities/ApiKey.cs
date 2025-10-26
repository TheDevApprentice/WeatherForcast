using domain.ValueObjects;

namespace domain.Entities
{
    /// <summary>
    /// Clé API pour accéder à l'API REST
    /// Utilise le standard OAuth2 (Client Credentials)
    /// Encapsulation renforcée avec traçabilité complète
    /// </summary>
    public class ApiKey
    {
        /// <summary>
        /// Identifiant unique
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Nom de la clé (ex: "Mon Application Mobile")
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// La clé API (client_id)
        /// Format: wf_live_xxxxxxxxxxxx (32 caractères aléatoires)
        /// </summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>
        /// Le secret de la clé (client_secret) - Hashé en base
        /// Affiché UNE SEULE FOIS à la création
        /// </summary>
        public string SecretHash { get; private set; } = string.Empty;

        /// <summary>
        /// Utilisateur propriétaire de la clé
        /// </summary>
        public string UserId { get; private set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// Date de dernière utilisation
        /// </summary>
        public DateTime? LastUsedAt { get; private set; }

        /// <summary>
        /// Date d'expiration (optionnel)
        /// </summary>
        public DateTime? ExpiresAt { get; private set; }

        /// <summary>
        /// La clé est-elle active ?
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// La clé est-elle révoquée ?
        /// </summary>
        public bool IsRevoked => RevokedAt.HasValue;

        /// <summary>
        /// Date de révocation/désactivation
        /// </summary>
        public DateTime? RevokedAt { get; private set; }

        /// <summary>
        /// Raison de la révocation/désactivation
        /// </summary>
        public string? RevocationReason { get; private set; }

        /// <summary>
        /// Scopes autorisés (Value Object)
        /// </summary>
        public ApiKeyScopes Scopes { get; private set; } = ApiKeyScopes.ReadWrite;

        /// <summary>
        /// Nombre total de requêtes effectuées avec cette clé
        /// </summary>
        public long RequestCount { get; private set; } = 0;

        /// <summary>
        /// Adresse IP autorisée (optionnel, pour restreindre l'usage)
        /// </summary>
        public string? AllowedIpAddress { get; private set; }

        /// <summary>
        /// Constructeur parameterless pour EF Core
        /// </summary>
        private ApiKey()
        {
        }

        /// <summary>
        /// Constructeur principal
        /// </summary>
        public ApiKey(string name, string key, string secretHash, string userId, ApiKeyScopes scopes, DateTime? expiresAt = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Le nom de la clé est requis", nameof(name));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("La clé API est requise", nameof(key));

            if (string.IsNullOrWhiteSpace(secretHash))
                throw new ArgumentException("Le hash du secret est requis", nameof(secretHash));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("L'utilisateur propriétaire est requis", nameof(userId));

            Name = name;
            Key = key;
            SecretHash = secretHash;
            UserId = userId;
            Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Enregistre une utilisation de la clé API
        /// Incrémente le compteur et met à jour la date de dernière utilisation
        /// </summary>
        public void RecordUsage()
        {
            if (!IsActive)
                throw new InvalidOperationException("Impossible d'utiliser une clé révoquée");

            if (IsExpired())
                throw new InvalidOperationException("La clé API a expiré");

            RequestCount++;
            LastUsedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Révoque (désactive) la clé API avec traçabilité
        /// </summary>
        public void Revoke(string reason)
        {
            if (!IsActive)
                throw new InvalidOperationException("La clé est déjà révoquée");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Une raison de révocation est requise", nameof(reason));

            IsActive = false;
            RevokedAt = DateTime.UtcNow;
            RevocationReason = reason;
        }

        /// <summary>
        /// Réactive une clé révoquée
        /// </summary>
        public void Reactivate()
        {
            if (IsActive)
                throw new InvalidOperationException("La clé est déjà active");

            IsActive = true;
            RevokedAt = null;
            RevocationReason = null;
        }

        /// <summary>
        /// Vérifie si la clé a expiré
        /// </summary>
        public bool IsExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
        }

        /// <summary>
        /// Vérifie si la clé est valide (active et non expirée)
        /// </summary>
        public bool IsValid()
        {
            return IsActive && !IsExpired();
        }

        /// <summary>
        /// Vérifie si la clé a un scope spécifique
        /// </summary>
        public bool HasScope(string scope)
        {
            return Scopes.HasScope(scope);
        }

        /// <summary>
        /// Met à jour les scopes de la clé
        /// </summary>
        public void UpdateScopes(ApiKeyScopes newScopes)
        {
            Scopes = newScopes ?? throw new ArgumentNullException(nameof(newScopes));
        }

        /// <summary>
        /// Prolonge la date d'expiration
        /// </summary>
        public void ExtendExpiration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentException("La durée doit être positive", nameof(duration));

            ExpiresAt = (ExpiresAt ?? DateTime.UtcNow).Add(duration);
        }

        /// <summary>
        /// Vérifie si l'IP est autorisée
        /// </summary>
        public bool IsIpAllowed(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(AllowedIpAddress))
                return true; // Pas de restriction IP

            return AllowedIpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Met à jour le nom de la clé API
        /// </summary>
        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Le nom ne peut pas être vide", nameof(newName));

            Name = newName;
        }

        /// <summary>
        /// Définit ou met à jour l'adresse IP autorisée
        /// </summary>
        public void SetAllowedIpAddress(string? ipAddress)
        {
            AllowedIpAddress = ipAddress;
        }

        /// <summary>
        /// Définit la date d'expiration
        /// </summary>
        public void SetExpiration(DateTime? expirationDate)
        {
            if (expirationDate.HasValue && expirationDate.Value < DateTime.UtcNow)
                throw new ArgumentException("La date d'expiration ne peut pas être dans le passé", nameof(expirationDate));

            ExpiresAt = expirationDate;
        }
    }
}

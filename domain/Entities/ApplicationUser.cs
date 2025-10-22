using Microsoft.AspNetCore.Identity;

namespace domain.Entities
{
    /// <summary>
    /// Utilisateur de l'application (hérite d'IdentityUser)
    /// Entité riche avec encapsulation et logique métier
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Propriétés personnalisées avec encapsulation
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Dernière connexion
        /// </summary>
        public DateTime? LastLoginAt { get; private set; }
        
        /// <summary>
        /// Compte actif ?
        /// </summary>
        public bool IsActive { get; private set; } = true;
        
        /// <summary>
        /// Date de désactivation
        /// </summary>
        public DateTime? DeactivatedAt { get; private set; }
        
        /// <summary>
        /// Raison de la désactivation
        /// </summary>
        public string? DeactivationReason { get; private set; }
        
        // Navigation properties
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

        /// <summary>
        /// Constructeur parameterless pour EF Core et Identity
        /// </summary>
        public ApplicationUser()
        {
            // EF Core et Identity ont besoin d'un constructeur sans paramètres
        }

        /// <summary>
        /// Constructeur pour créer un nouvel utilisateur
        /// </summary>
        public ApplicationUser(string email, string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("L'email est requis", nameof(email));

            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("Le prénom est requis", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Le nom est requis", nameof(lastName));

            Email = email;
            UserName = email;
            FirstName = firstName;
            LastName = lastName;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Nom complet de l'utilisateur
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Mettre à jour les informations personnelles
        /// </summary>
        public void UpdatePersonalInfo(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("Le prénom est requis", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Le nom est requis", nameof(lastName));

            FirstName = firstName;
            LastName = lastName;
        }

        /// <summary>
        /// Enregistrer une connexion
        /// </summary>
        public void RecordLogin()
        {
            if (!IsActive)
                throw new InvalidOperationException("Impossible de se connecter : le compte est désactivé");

            LastLoginAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Désactiver le compte
        /// </summary>
        public void Deactivate(string reason)
        {
            if (!IsActive)
                throw new InvalidOperationException("Le compte est déjà désactivé");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("La raison de désactivation est requise", nameof(reason));

            IsActive = false;
            DeactivatedAt = DateTime.UtcNow;
            DeactivationReason = reason;
        }

        /// <summary>
        /// Réactiver le compte
        /// </summary>
        public void Reactivate()
        {
            if (IsActive)
                throw new InvalidOperationException("Le compte est déjà actif");

            IsActive = true;
            DeactivatedAt = null;
            DeactivationReason = null;
        }

        /// <summary>
        /// Vérifier si l'utilisateur est nouveau (jamais connecté)
        /// </summary>
        public bool IsNewUser() => !LastLoginAt.HasValue;

        /// <summary>
        /// Vérifier si l'utilisateur est inactif depuis X jours
        /// </summary>
        public bool IsInactiveSince(int days)
        {
            if (!LastLoginAt.HasValue)
                return false;

            return (DateTime.UtcNow - LastLoginAt.Value).TotalDays > days;
        }
    }
}

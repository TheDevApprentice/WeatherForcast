using Microsoft.AspNetCore.Identity;

namespace domain.Entities
{
    /// <summary>
    /// Utilisateur de l'application (hérite d'IdentityUser)
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Propriétés personnalisées
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Dernière connexion
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
        
        /// <summary>
        /// Compte actif ?
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    }
}

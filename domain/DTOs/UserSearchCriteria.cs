namespace domain.DTOs
{
    /// <summary>
    /// Critères de recherche et filtrage des utilisateurs
    /// </summary>
    public class UserSearchCriteria
    {
        /// <summary>
        /// Recherche textuelle (nom, prénom, email)
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filtrer par statut actif/inactif
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filtrer par rôle
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Date de création minimale
        /// </summary>
        public DateTime? CreatedAfter { get; set; }

        /// <summary>
        /// Date de création maximale
        /// </summary>
        public DateTime? CreatedBefore { get; set; }

        /// <summary>
        /// Date de dernière connexion minimale
        /// </summary>
        public DateTime? LastLoginAfter { get; set; }

        /// <summary>
        /// Date de dernière connexion maximale
        /// </summary>
        public DateTime? LastLoginBefore { get; set; }

        /// <summary>
        /// Tri (CreatedAt, LastLoginAt, Email, etc.)
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Ordre de tri (asc, desc)
        /// </summary>
        public bool SortDescending { get; set; } = true;

        /// <summary>
        /// Numéro de page (commence à 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Nombre d'éléments par page
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

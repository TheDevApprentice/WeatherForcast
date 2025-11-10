namespace domain.DTOs
{
    /// <summary>
    /// Résultat paginé générique pour les listes de données
    /// </summary>
    /// <typeparam name="T">Type des éléments de la liste</typeparam>
    /// <remarks>
    /// Utilisé pour paginer les résultats des endpoints GET qui retournent des listes.
    /// Permet de limiter la quantité de données transférées et d'améliorer les performances.
    /// </remarks>
    /// <example>
    /// GET /api/weatherforecast?page=1&amp;pageSize=20
    /// </example>
    public class PagedResult<T>
    {
        /// <summary>
        /// Liste des éléments de la page courante
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();
        
        /// <summary>
        /// Nombre total d'éléments (toutes pages confondues)
        /// </summary>
        /// <example>150</example>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Numéro de la page courante (commence à 1)
        /// </summary>
        /// <example>1</example>
        public int PageNumber { get; set; }
        
        /// <summary>
        /// Nombre d'éléments par page
        /// </summary>
        /// <example>20</example>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Nombre total de pages
        /// </summary>
        /// <remarks>Calculé automatiquement : Ceiling(TotalCount / PageSize)</remarks>
        /// <example>8</example>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        
        /// <summary>
        /// Indique s'il existe une page précédente
        /// </summary>
        /// <remarks>True si PageNumber > 1</remarks>
        public bool HasPreviousPage => PageNumber > 1;
        
        /// <summary>
        /// Indique s'il existe une page suivante
        /// </summary>
        /// <remarks>True si PageNumber &lt; TotalPages</remarks>
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

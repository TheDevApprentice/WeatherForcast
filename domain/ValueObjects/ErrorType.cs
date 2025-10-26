namespace domain.ValueObjects
{
    /// <summary>
    /// Types d'erreurs possibles dans le domain
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Erreur de validation des données
        /// </summary>
        Validation,

        /// <summary>
        /// Erreur de base de données
        /// </summary>
        Database,

        /// <summary>
        /// Erreur d'un service externe (Email, Redis, etc.)
        /// </summary>
        External,

        /// <summary>
        /// Erreur de permission/autorisation
        /// </summary>
        Authorization,

        /// <summary>
        /// Entité non trouvée
        /// </summary>
        NotFound,

        /// <summary>
        /// Erreur inconnue
        /// </summary>
        Unknown
    }
}

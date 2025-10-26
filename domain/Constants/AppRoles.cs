namespace domain.Constants
{
    /// <summary>
    /// Rôles de l'application
    /// </summary>
    public static class AppRoles
    {
        /// <summary>
        /// Administrateur - Accès complet
        /// </summary>
        public const string Admin = "Admin";
        
        /// <summary>
        /// Utilisateur standard - Accès CRUD forecasts + ses API keys
        /// </summary>
        public const string User = "User";
        
        /// <summary>
        /// Consommateur API - Accès via API Key uniquement (read-only)
        /// </summary>
        public const string ApiConsumer = "ApiConsumer";
        
        /// <summary>
        /// Utilisateur mobile - Accès via JWT (login mobile)
        /// </summary>
        public const string MobileUser = "MobileUser";
        
        /// <summary>
        /// Tous les rôles
        /// </summary>
        public static readonly string[] All = { Admin, User, ApiConsumer, MobileUser };
        
        /// <summary>
        /// Rôles avec accès Web
        /// </summary>
        public static readonly string[] WebAccess = { Admin, User };
        
        /// <summary>
        /// Rôles avec accès API
        /// </summary>
        public static readonly string[] ApiAccess = { Admin, ApiConsumer, MobileUser };
    }
}

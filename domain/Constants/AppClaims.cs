namespace domain.Constants
{
    /// <summary>
    /// Claims personnalisés de l'application
    /// </summary>
    public static class AppClaims
    {
        // ========== Types de Claims ==========
        
        /// <summary>
        /// Type de claim pour les permissions
        /// </summary>
        public const string Permission = "permission";
        
        /// <summary>
        /// Type de claim pour le type d'accès
        /// </summary>
        public const string AccessType = "access_type";
        
        // ========== Permissions Forecasts ==========
        
        /// <summary>
        /// Lire les prévisions météo
        /// </summary>
        public const string ForecastRead = "forecast:read";
        
        /// <summary>
        /// Créer/modifier les prévisions météo
        /// </summary>
        public const string ForecastWrite = "forecast:write";
        
        /// <summary>
        /// Supprimer les prévisions météo
        /// </summary>
        public const string ForecastDelete = "forecast:delete";
        
        // ========== Permissions API Keys ==========
        
        /// <summary>
        /// Gérer ses propres clés API
        /// </summary>
        public const string ApiKeyManage = "apikey:manage";
        
        /// <summary>
        /// Voir toutes les clés API (admin)
        /// </summary>
        public const string ApiKeyViewAll = "apikey:viewall";
        
        // ========== Permissions Utilisateurs ==========
        
        /// <summary>
        /// Gérer les utilisateurs (admin)
        /// </summary>
        public const string UserManage = "user:manage";
        
        /// <summary>
        /// Voir tous les utilisateurs
        /// </summary>
        public const string UserViewAll = "user:viewall";
        
        // ========== Types d'Accès ==========
        
        /// <summary>
        /// Accès via Web (Cookie)
        /// </summary>
        public const string WebAccess = "web";
        
        /// <summary>
        /// Accès via API (JWT)
        /// </summary>
        public const string ApiAccess = "api";
        
        /// <summary>
        /// Accès via API Key (Basic Auth)
        /// </summary>
        public const string ApiKeyAccess = "apikey";
        
        // ========== Groupes de Permissions ==========
        
        /// <summary>
        /// Toutes les permissions Forecast
        /// </summary>
        public static readonly string[] ForecastPermissions = 
        {
            ForecastRead,
            ForecastWrite,
            ForecastDelete
        };
        
        /// <summary>
        /// Toutes les permissions API Key
        /// </summary>
        public static readonly string[] ApiKeyPermissions = 
        {
            ApiKeyManage,
            ApiKeyViewAll
        };
        
        /// <summary>
        /// Toutes les permissions User
        /// </summary>
        public static readonly string[] UserPermissions = 
        {
            UserManage,
            UserViewAll
        };
        
        /// <summary>
        /// Toutes les permissions
        /// </summary>
        public static readonly string[] AllPermissions = 
        {
            ForecastRead,
            ForecastWrite,
            ForecastDelete,
            ApiKeyManage,
            ApiKeyViewAll,
            UserManage,
            UserViewAll
        };
    }
}

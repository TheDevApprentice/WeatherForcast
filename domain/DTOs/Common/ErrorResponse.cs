namespace domain.DTOs.Common
{
    /// <summary>
    /// Réponse d'erreur standardisée pour l'API
    /// Utilisé par API, Application et Mobile
    /// </summary>
    /// <remarks>
    /// Format standard pour toutes les erreurs HTTP 4xx et 5xx.
    /// Permet une gestion cohérente des erreurs côté client.
    /// </remarks>
    /// <example>
    /// HTTP 401 Unauthorized
    /// {
    ///   "error": "unauthorized",
    ///   "message": "Token invalide ou expiré",
    ///   "documentation": "https://api.example.com/docs/errors#unauthorized"
    /// }
    /// </example>
    public class ErrorResponse
    {
        /// <summary>
        /// Code d'erreur machine-readable
        /// </summary>
        /// <remarks>
        /// Codes courants :
        /// - "unauthorized" : Non authentifié (401)
        /// - "forbidden" : Accès refusé (403)
        /// - "not_found" : Ressource introuvable (404)
        /// - "invalid_request" : Requête invalide (400)
        /// - "validation_error" : Erreur de validation (400)
        /// - "internal_error" : Erreur serveur (500)
        /// </remarks>
        /// <example>unauthorized</example>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Message d'erreur human-readable
        /// </summary>
        /// <remarks>
        /// Message descriptif destiné à être affiché à l'utilisateur final.
        /// Doit être clair et actionnable.
        /// </remarks>
        /// <example>Token invalide ou expiré. Veuillez vous reconnecter.</example>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Lien vers la documentation de l'erreur (optionnel)
        /// </summary>
        /// <remarks>
        /// URL vers la documentation expliquant l'erreur et comment la résoudre.
        /// </remarks>
        /// <example>https://api.example.com/docs/errors#unauthorized</example>
        public string? Documentation { get; set; }
    }
}

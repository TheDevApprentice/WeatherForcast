namespace api.DTOs
{
    /// <summary>
    /// Réponse d'erreur standardisée pour l'API
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Code d'erreur (ex: "unauthorized", "invalid_request", "not_found")
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Message d'erreur détaillé
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Lien vers la documentation (optionnel)
        /// </summary>
        public string? Documentation { get; set; }
    }
}

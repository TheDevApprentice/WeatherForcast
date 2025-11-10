namespace domain.DTOs.WeatherForecast
{
    /// <summary>
    /// DTO pour la mise à jour d'une prévision météo
    /// L'ID est passé dans l'URL, pas dans le body
    /// Validation déléguée à FluentValidation (UpdateWeatherForecastRequestValidator)
    /// Utilisé par API, Application et Mobile
    /// </summary>
    /// <remarks>
    /// Endpoint: PUT /api/weatherforecast/{id}
    /// Authentification: JWT Bearer (Policy: ForecastWrite)
    /// Permissions requises: forecast:write
    /// </remarks>
    public class UpdateWeatherForecastRequest
    {
        /// <summary>
        /// Date de la prévision
        /// </summary>
        /// <remarks>
        /// Contraintes de validation :
        /// - Ne peut pas être antérieure à 1 an
        /// - Ne peut pas être supérieure à 1 an dans le futur
        /// </remarks>
        /// <example>2025-11-15</example>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Température en Celsius
        /// </summary>
        /// <remarks>
        /// Contraintes de validation :
        /// - Doit être entre -100°C et 100°C
        /// </remarks>
        /// <example>28</example>
        public int TemperatureC { get; set; }
        
        /// <summary>
        /// Résumé météo (ex: "Hot", "Cold", "Mild")
        /// </summary>
        /// <remarks>
        /// Contraintes de validation :
        /// - Requis (non vide)
        /// - Maximum 50 caractères
        /// </remarks>
        /// <example>Partly Cloudy</example>
        public string Summary { get; set; } = string.Empty;
    }
}

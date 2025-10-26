namespace api.DTOs
{
    /// <summary>
    /// DTO pour la mise à jour d'une prévision météo
    /// L'ID est passé dans l'URL, pas dans le body
    /// Validation déléguée à FluentValidation (UpdateWeatherForecastRequestValidator)
    /// </summary>
    public class UpdateWeatherForecastRequest
    {
        /// <summary>
        /// Date de la prévision
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Température en Celsius
        /// </summary>
        public int TemperatureC { get; set; }
        
        /// <summary>
        /// Résumé météo (ex: "Hot", "Cold", "Mild")
        /// </summary>
        public string Summary { get; set; } = string.Empty;
    }
}

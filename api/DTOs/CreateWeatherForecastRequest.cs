namespace api.DTOs
{
    /// <summary>
    /// DTO pour la création d'une prévision météo
    /// L'ID est auto-généré par EF Core, donc absent du DTO
    /// Validation déléguée à FluentValidation (CreateWeatherForecastRequestValidator)
    /// </summary>
    public class CreateWeatherForecastRequest
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

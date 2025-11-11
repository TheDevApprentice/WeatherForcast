namespace mobile.Models
{
    /// <summary>
    /// Modèle de prévision météo (synchronisé avec l'API)
    /// Correspond à l'entité domain.Entities.WeatherForecast
    /// </summary>
    public class WeatherForecast
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Température en Celsius (correspond à TemperatureC de l'API)
        /// </summary>
        public int TemperatureC { get; set; }
        
        /// <summary>
        /// Température en Fahrenheit (correspond à TemperatureF de l'API)
        /// </summary>
        public int TemperatureF { get; set; }
        
        public string? Summary { get; set; }

        /// <summary>
        /// Indique si la température est chaude (superior or equal 20°C)
        /// Propriété calculée côté client
        /// </summary>
        public bool IsHot => TemperatureC >= 20;

        /// <summary>
        /// Indique si la température est froide (inferior or equal 0°C)
        /// Propriété calculée côté client
        /// </summary>
        public bool IsCold => TemperatureC <= 0;

        /// <summary>
        /// Couleur associée à la température pour l'affichage
        /// Propriété calculée côté client
        /// </summary>
        public string TemperatureColor
        {
            get
            {
                if (IsCold) return "#3b82f6"; // Bleu
                if (IsHot) return "#ef4444";  // Rouge
                return "#10b981";             // Vert (tempéré)
            }
        }
    }
}

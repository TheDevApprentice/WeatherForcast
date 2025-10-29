namespace mobile.Models
{
    /// <summary>
    /// Modèle de prévision météo (synchronisé avec l'API)
    /// </summary>
    public class WeatherForecast
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string? Summary { get; set; }

        /// <summary>
        /// Indique si la température est chaude (>= 20°C)
        /// </summary>
        public bool IsHot => TemperatureC >= 20;

        /// <summary>
        /// Indique si la température est froide (<= 0°C)
        /// </summary>
        public bool IsCold => TemperatureC <= 0;

        /// <summary>
        /// Couleur associée à la température pour l'affichage
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

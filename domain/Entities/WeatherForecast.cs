namespace domain.Entities
{
    /// <summary>
    /// Entité métier WeatherForecast (Domain Entity)
    /// Logique métier pure, aucune dépendance externe
    /// </summary>
    public class WeatherForecast
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }

        // Propriété calculée (logique métier)
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        // Méthode métier (exemple)
        public bool IsHot() => TemperatureC > 30;
        
        public bool IsCold() => TemperatureC < 0;
    }
}

using SQLite;

namespace mobile.Models.Cache
{
    /// <summary>
    /// Modèle pour stocker une prévision météo dans le cache SQLite
    /// </summary>
    [Table("CachedForecasts")]
    public class CachedForecast
    {
        /// <summary>
        /// ID de la prévision (clé primaire)
        /// </summary>
        [PrimaryKey]
        public int Id { get; set; }

        /// <summary>
        /// Date de la prévision
        /// </summary>
        [NotNull]
        public DateTime Date { get; set; }

        /// <summary>
        /// Température en Celsius
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Température en Fahrenheit
        /// </summary>
        public int TemperatureF { get; set; }

        /// <summary>
        /// Indique si la température est chaude (>= 20°C)
        /// Propriété calculée côté client
        /// </summary>
        public bool IsHot { get; set; }

        /// <summary>
        /// Indique si la température est froide (<= 0°C)
        /// Propriété calculée côté client
        /// </summary>
        public bool IsCold { get; set; }
        
        /// <summary>
        /// Résumé de la météo
        /// </summary>
        public string? Summary { get; set; }

        // Note: IsHot et IsCold sont des propriétés calculées dans WeatherForecast
        // On ne les stocke pas en cache car elles sont dérivées de TemperatureC

        /// <summary>
        /// Date de mise en cache
        /// </summary>
        [NotNull]
        public DateTime CachedAt { get; set; }

        /// <summary>
        /// Convertit un WeatherForecast en CachedForecast
        /// </summary>
        public static CachedForecast FromWeatherForecast(WeatherForecast forecast)
        {
            return new CachedForecast
            {
                Id = forecast.Id,
                Date = forecast.Date,
                TemperatureC = forecast.TemperatureC,
                TemperatureF = forecast.TemperatureF,
                Summary = forecast.Summary,
                IsHot = forecast.IsHot,
                IsCold = forecast.IsCold,
                CachedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Convertit un CachedForecast en WeatherForecast
        /// </summary>
        public WeatherForecast ToWeatherForecast()
        {
            return new WeatherForecast
            {
                Id = Id,
                Date = Date,
                TemperatureC = TemperatureC,
                TemperatureF = TemperatureF,
                Summary = Summary
                // IsHot et IsCold sont calculés automatiquement par WeatherForecast
            };
        }
    }
}

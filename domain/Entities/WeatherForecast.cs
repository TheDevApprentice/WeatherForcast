using domain.ValueObjects;

namespace domain.Entities
{
    /// <summary>
    /// Entité métier WeatherForecast (Domain Entity)
    /// Utilise le Value Object Temperature pour une meilleure encapsulation
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// Identifiant unique
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Date de la prévision
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Température (Value Object)
        /// Encapsule la logique métier liée à la température
        /// </summary>
        public Temperature Temperature { get; private set; } = null!;

        /// <summary>
        /// Résumé de la météo (Sunny, Rainy, etc.)
        /// </summary>
        public string? Summary { get; private set; }

        /// <summary>
        /// Constructeur parameterless pour EF Core
        /// </summary>
        private WeatherForecast()
        {
        }

        /// <summary>
        /// Constructeur principal avec validation
        /// </summary>
        public WeatherForecast(DateTime date, Temperature temperature, string? summary)
        {
            ValidateDate(date);
            Date = date;
            Temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
            Summary = summary;
        }

        /// <summary>
        /// Met à jour la température
        /// </summary>
        public void UpdateTemperature(Temperature newTemperature)
        {
            Temperature = newTemperature ?? throw new ArgumentNullException(nameof(newTemperature));
        }

        /// <summary>
        /// Met à jour le résumé
        /// </summary>
        public void UpdateSummary(string? newSummary)
        {
            Summary = newSummary;
        }

        /// <summary>
        /// Met à jour la date
        /// </summary>
        public void UpdateDate(DateTime newDate)
        {
            ValidateDate(newDate);
            Date = newDate;
        }

        /// <summary>
        /// Validation de la date
        /// </summary>
        private static void ValidateDate(DateTime date)
        {
            if (date < DateTime.UtcNow.AddYears(-1))
                throw new ArgumentException("La date ne peut pas être antérieure à 1 an");

            if (date > DateTime.UtcNow.AddYears(1))
                throw new ArgumentException("La date ne peut pas être supérieure à 1 an dans le futur");
        }

        // Propriétés de commodité pour accéder aux propriétés du Value Object
        /// <summary>
        /// Température en Celsius (raccourci)
        /// </summary>
        public int TemperatureC => Temperature.Celsius;

        /// <summary>
        /// Température en Fahrenheit (raccourci)
        /// </summary>
        public int TemperatureF => Temperature.Fahrenheit;

        /// <summary>
        /// Indique si la température est chaude
        /// </summary>
        public bool IsHot() => Temperature.IsHot;

        /// <summary>
        /// Indique si la température est froide
        /// </summary>
        public bool IsCold() => Temperature.IsCold;
    }
}

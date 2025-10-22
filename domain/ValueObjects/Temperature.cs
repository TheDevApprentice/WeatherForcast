namespace domain.ValueObjects
{
    /// <summary>
    /// Value Object représentant une température
    /// Immutable et avec validation intégrée
    /// </summary>
    public record Temperature
    {
        private const int MinTemperature = -100;
        private const int MaxTemperature = 100;

        /// <summary>
        /// Température en Celsius
        /// </summary>
        public int Celsius { get; init; }

        /// <summary>
        /// Température en Fahrenheit (calculée)
        /// </summary>
        public int Fahrenheit => 32 + (int)(Celsius / 0.5556);

        /// <summary>
        /// Indique si la température est chaude (> 30°C)
        /// </summary>
        public bool IsHot => Celsius > 30;

        /// <summary>
        /// Indique si la température est froide (< 0°C)
        /// </summary>
        public bool IsCold => Celsius < 0;

        /// <summary>
        /// Constructeur principal avec validation
        /// </summary>
        /// <param name="celsius">Température en Celsius</param>
        /// <exception cref="ArgumentException">Si la température est hors limites</exception>
        public Temperature(int celsius)
        {
            if (celsius < MinTemperature || celsius > MaxTemperature)
                throw new ArgumentException(
                    $"La température doit être entre {MinTemperature}°C et {MaxTemperature}°C. Valeur reçue : {celsius}°C");

            Celsius = celsius;
        }

        /// <summary>
        /// Constructeur parameterless privé pour EF Core
        /// </summary>
        private Temperature()
        {
            Celsius = 0;
        }

        /// <summary>
        /// Crée une température depuis une valeur Fahrenheit
        /// </summary>
        public static Temperature FromFahrenheit(int fahrenheit)
        {
            int celsius = (int)((fahrenheit - 32) * 0.5556);
            return new Temperature(celsius);
        }

        /// <summary>
        /// Retourne une représentation textuelle de la température
        /// </summary>
        public override string ToString() => $"{Celsius}°C ({Fahrenheit}°F)";
    }
}

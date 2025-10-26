namespace domain.Events.WeatherForecast
{
    /// <summary>
    /// Event déclenché lorsqu'une prévision météo est créée
    /// </summary>
    public class ForecastCreatedEvent : INotification
    {
        public Entities.WeatherForecast Forecast { get; }
        public string? TriggeredBy { get; }
        public string? ExcludedConnectionId { get; }
        public DateTime Timestamp { get; }

        public ForecastCreatedEvent(
            Entities.WeatherForecast forecast,
            string? triggeredBy = null,
            string? excludedConnectionId = null)
        {
            Forecast = forecast;
            TriggeredBy = triggeredBy;
            ExcludedConnectionId = excludedConnectionId;
            Timestamp = DateTime.UtcNow;
        }
    }
}

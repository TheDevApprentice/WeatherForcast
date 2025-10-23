namespace domain.Events.WeatherForecast
{
    /// <summary>
    /// Event déclenché lorsqu'une prévision météo est mise à jour
    /// </summary>
    public class ForecastUpdatedEvent : INotification
    {
        public Entities.WeatherForecast Forecast { get; }
        public string? TriggeredBy { get; }
        public string? ExcludedConnectionId { get; }
        public DateTime Timestamp { get; }

        public ForecastUpdatedEvent(
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

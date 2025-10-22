using MediatR;

namespace domain.Events.WeatherForecast
{
    /// <summary>
    /// Event déclenché lorsqu'une prévision météo est supprimée
    /// </summary>
    public class ForecastDeletedEvent : INotification
    {
        public int Id { get; }
        public string? TriggeredBy { get; }
        public DateTime Timestamp { get; }

        public ForecastDeletedEvent(
            int id, 
            string? triggeredBy = null)
        {
            Id = id;
            TriggeredBy = triggeredBy;
            Timestamp = DateTime.UtcNow;
        }
    }
}

namespace mobile.Models.DTOs
{
    /// <summary>
    /// Requête de mise à jour de prévision météo
    /// </summary>
    public class UpdateForecastRequest
    {
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }
    }
}

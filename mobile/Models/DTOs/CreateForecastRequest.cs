namespace mobile.Models.DTOs
{
    /// <summary>
    /// Requête de création de prévision météo
    /// </summary>
    public class CreateForecastRequest
    {
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }
    }
}

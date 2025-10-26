using System.ComponentModel.DataAnnotations;

namespace application.ViewModels
{
    /// <summary>
    /// ViewModel pour la création et l'édition de prévisions météo
    /// Utilisé pour le binding des formulaires
    /// Validation déléguée à FluentValidation (WeatherForecastViewModelValidator)
    /// </summary>
    public class WeatherForecastViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Température (°C)")]
        public int TemperatureC { get; set; }

        [Display(Name = "Résumé")]
        public string? Summary { get; set; }
    }
}

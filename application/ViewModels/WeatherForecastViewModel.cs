using System.ComponentModel.DataAnnotations;

namespace application.ViewModels
{
    /// <summary>
    /// ViewModel pour la création et l'édition de prévisions météo
    /// Utilisé pour le binding des formulaires
    /// </summary>
    public class WeatherForecastViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La date est requise")]
        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        [Required(ErrorMessage = "La température est requise")]
        [Range(-100, 100, ErrorMessage = "La température doit être entre -100°C et 100°C")]
        [Display(Name = "Température (°C)")]
        public int TemperatureC { get; set; }

        [MaxLength(200, ErrorMessage = "Le résumé ne peut pas dépasser 200 caractères")]
        [Display(Name = "Résumé")]
        public string? Summary { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /// <summary>
    /// DTO pour la création d'une prévision météo
    /// L'ID est auto-généré par EF Core, donc absent du DTO
    /// </summary>
    public class CreateWeatherForecastRequest
    {
        /// <summary>
        /// Date de la prévision
        /// </summary>
        [Required(ErrorMessage = "La date est requise")]
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Température en Celsius
        /// </summary>
        [Required(ErrorMessage = "La température est requise")]
        [Range(-100, 100, ErrorMessage = "La température doit être entre -100°C et 100°C")]
        public int TemperatureC { get; set; }
        
        /// <summary>
        /// Résumé météo (ex: "Hot", "Cold", "Mild")
        /// </summary>
        [Required(ErrorMessage = "Le résumé est requis")]
        [StringLength(100, ErrorMessage = "Le résumé ne peut pas dépasser 100 caractères")]
        public string Summary { get; set; } = string.Empty;
    }
}

using domain.Entities;

namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service de gestion CRUD des prévisions météo
    /// </summary>
    public interface IWeatherForecastService
    {
        /// <summary>
        /// Récupérer toutes les prévisions météo
        /// </summary>
        Task<IEnumerable<WeatherForecast>> GetAllAsync();

        /// <summary>
        /// Récupérer une prévision par ID
        /// </summary>
        Task<WeatherForecast?> GetByIdAsync(int id);

        /// <summary>
        /// Créer une nouvelle prévision météo
        /// </summary>
        Task<WeatherForecast> CreateAsync(WeatherForecast forecast);

        /// <summary>
        /// Mettre à jour une prévision existante
        /// </summary>
        Task<bool> UpdateAsync(int id, WeatherForecast forecast);

        /// <summary>
        /// Supprimer une prévision météo
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}

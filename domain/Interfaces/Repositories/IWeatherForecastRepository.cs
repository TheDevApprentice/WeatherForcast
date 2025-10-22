using domain.Entities;

namespace domain.Interfaces.Repositories
{
    /// <summary>
    /// Interface Repository pour WeatherForecast (Port)
    /// Définit le contrat pour l'accès aux données
    /// </summary>
    public interface IWeatherForecastRepository
    {
        Task<IEnumerable<WeatherForecast>> GetAllAsync();
        Task<WeatherForecast?> GetByIdAsync(int id);
        Task<IEnumerable<WeatherForecast>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task AddAsync(WeatherForecast forecast);
        void Update(WeatherForecast forecast);
        void Delete(WeatherForecast forecast);
    }
}

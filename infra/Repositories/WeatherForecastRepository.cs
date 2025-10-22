using Microsoft.EntityFrameworkCore;
using domain.Entities;
using infra.Data;
using domain.Interfaces.Repositories;

namespace infra.Repositories
{
    /// <summary>
    /// Implémentation du Repository pour WeatherForecast (Adapter)
    /// Gère l'accès aux données via EF Core
    /// </summary>
    public class WeatherForecastRepository : IWeatherForecastRepository
    {
        private readonly AppDbContext _context;

        public WeatherForecastRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WeatherForecast>> GetAllAsync()
        {
            return await _context.WeatherForecasts
                .OrderBy(w => w.Date)
                .ToListAsync();
        }

        public async Task<WeatherForecast?> GetByIdAsync(int id)
        {
            return await _context.WeatherForecasts
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<WeatherForecast>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.WeatherForecasts
                .Where(w => w.Date >= startDate && w.Date <= endDate)
                .OrderBy(w => w.Date)
                .ToListAsync();
        }

        public async Task AddAsync(WeatherForecast forecast)
        {
            await _context.WeatherForecasts.AddAsync(forecast);
        }

        public void Update(WeatherForecast forecast)
        {
            _context.WeatherForecasts.Update(forecast);
        }

        public void Delete(WeatherForecast forecast)
        {
            _context.WeatherForecasts.Remove(forecast);
        }
    }
}

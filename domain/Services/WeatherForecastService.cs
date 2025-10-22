using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Services;
using domain.Events.WeatherForecast;
using MediatR;

namespace domain.Services
{
    /// <summary>
    /// Service de gestion CRUD des prévisions météo
    /// Utilise le UnitOfWork pour gérer les transactions
    /// Publie des domain events via MediatR pour les notifications temps réel
    /// </summary>
    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;

        public WeatherForecastService(
            IUnitOfWork unitOfWork,
            IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _publisher = publisher;
        }

        public async Task<IEnumerable<WeatherForecast>> GetAllAsync()
        {
            return await _unitOfWork.WeatherForecasts.GetAllAsync();
        }

        public async Task<WeatherForecast?> GetByIdAsync(int id)
        {
            return await _unitOfWork.WeatherForecasts.GetByIdAsync(id);
        }

        public async Task<WeatherForecast> CreateAsync(WeatherForecast forecast)
        {
            await _unitOfWork.WeatherForecasts.AddAsync(forecast);
            await _unitOfWork.SaveChangesAsync();
            
            // Publier l'event pour notifier tous les handlers (SignalR, Audit, etc.)
            await _publisher.Publish(new ForecastCreatedEvent(forecast));
            
            return forecast;
        }

        public async Task<bool> UpdateAsync(int id, WeatherForecast forecast)
        {
            var existingForecast = await _unitOfWork.WeatherForecasts.GetByIdAsync(id);
            
            if (existingForecast == null)
            {
                return false;
            }

            // Mettre à jour les propriétés de l'entité trackée (pas de conflit)
            existingForecast.Date = forecast.Date;
            existingForecast.TemperatureC = forecast.TemperatureC;
            existingForecast.Summary = forecast.Summary;

            await _unitOfWork.SaveChangesAsync();
            
            // Publier l'event pour notifier tous les handlers (utiliser l'entité trackée)
            await _publisher.Publish(new ForecastUpdatedEvent(existingForecast));
            
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var forecast = await _unitOfWork.WeatherForecasts.GetByIdAsync(id);
            
            if (forecast == null)
            {
                return false;
            }

            _unitOfWork.WeatherForecasts.Delete(forecast);
            await _unitOfWork.SaveChangesAsync();
            
            // Publier l'event pour notifier tous les handlers
            await _publisher.Publish(new ForecastDeletedEvent(id));
            
            return true;
        }
    }
}

using domain.Entities;
using domain.Events;
using domain.Events.WeatherForecast;
using domain.Interfaces;
using domain.Interfaces.Services;
using domain.ValueObjects;

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
        private readonly ISignalRConnectionService _connectionService;

        public WeatherForecastService(
            IUnitOfWork unitOfWork,
            IPublisher publisher,
            ISignalRConnectionService connectionService)
        {
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _connectionService = connectionService;
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

            // Récupérer le ConnectionId de l'émetteur pour l'exclure des notifications
            var excludedConnectionId = _connectionService.GetCurrentConnectionId();

            // Publier l'event pour notifier tous les handlers (SignalR, Audit, etc.)
            await _publisher.Publish(new ForecastCreatedEvent(forecast, excludedConnectionId: excludedConnectionId));

            return forecast;
        }

        public async Task<bool> UpdateAsync(int id, DateTime date, Temperature temperature, string? summary)
        {
            var existingForecast = await _unitOfWork.WeatherForecasts.GetByIdAsync(id);

            if (existingForecast == null)
            {
                return false;
            }

            // Mettre à jour les propriétés via les méthodes de l'entité
            existingForecast.UpdateDate(date);
            existingForecast.UpdateTemperature(temperature);
            existingForecast.UpdateSummary(summary);

            await _unitOfWork.SaveChangesAsync();

            // Récupérer le ConnectionId de l'émetteur pour l'exclure des notifications
            var excludedConnectionId = _connectionService.GetCurrentConnectionId();

            // Publier l'event pour notifier tous les handlers (utiliser l'entité trackée)
            await _publisher.Publish(new ForecastUpdatedEvent(existingForecast, excludedConnectionId: excludedConnectionId));

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

            // Récupérer le ConnectionId de l'émetteur pour l'exclure des notifications
            var excludedConnectionId = _connectionService.GetCurrentConnectionId();

            // Publier l'event pour notifier tous les handlers
            await _publisher.Publish(new ForecastDeletedEvent(id, excludedConnectionId: excludedConnectionId));

            return true;
        }
    }
}

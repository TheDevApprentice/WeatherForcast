using domain.Interfaces.Repos;
using domain.Interfaces.Repositories;

namespace domain.Interfaces
{
    /// <summary>
    /// Interface Unit of Work (Port)
    /// Gère les transactions et coordonne les repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repositories
        IWeatherForecastRepository WeatherForecasts { get; }
        IUserRepository Users { get; }
        ISessionRepository Sessions { get; }
        IApiKeyRepository ApiKeys { get; }

        // Gestion des transactions
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}

using Microsoft.EntityFrameworkCore.Storage;
using domain.Interfaces;
using domain.Interfaces.Repositories;
using infra.Repositories;

namespace infra.Data
{
    /// <summary>
    /// Implémentation du pattern Unit of Work
    /// Coordonne les repositories et gère les transactions
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repositories (lazy initialization)
        private IWeatherForecastRepository? _weatherForecasts;
        private IUserRepository? _users;
        private ISessionRepository? _sessions;
        private IApiKeyRepository? _apiKeys;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // Propriétés des repositories
        public IWeatherForecastRepository WeatherForecasts
        {
            get
            {
                _weatherForecasts ??= new WeatherForecastRepository(_context);
                return _weatherForecasts;
            }
        }

        public IUserRepository Users
        {
            get
            {
                _users ??= new UserRepository(_context);
                return _users;
            }
        }

        public ISessionRepository Sessions
        {
            get
            {
                _sessions ??= new SessionRepository(_context);
                return _sessions;
            }
        }

        public IApiKeyRepository ApiKeys
        {
            get
            {
                _apiKeys ??= new ApiKeyRepository(_context);
                return _apiKeys;
            }
        }

        // Sauvegarde des changements
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // Gestion des transactions
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Dispose
        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}

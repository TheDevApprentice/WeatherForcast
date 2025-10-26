using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace shared.Messaging
{
    public class EventPublisher : IPublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<INotificationHandler<TNotification>>().ToList();

            if (handlers.Count == 0)
            {
                _logger.LogDebug("No handlers registered for {EventType}", typeof(TNotification).FullName);
                return;
            }

            var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Event"] = typeof(TNotification).Name
            });

            var totalSw = Stopwatch.StartNew();

            var tasks = handlers.Select(async handler =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await handler.Handle(notification, cancellationToken);
                    sw.Stop();
                    _logger.LogInformation("Handled {EventType} with {Handler} in {DurationMs} ms",
                        typeof(TNotification).FullName,
                        handler.GetType().FullName,
                        sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex, "Error while handling event {EventType} with {Handler} after {DurationMs} ms",
                        typeof(TNotification).FullName,
                        handler.GetType().FullName,
                        sw.ElapsedMilliseconds);
                    // Ne pas throw pour ne pas bloquer les autres handlers
                }
            });

            await Task.WhenAll(tasks);

            totalSw.Stop();
            _logger.LogInformation("Published {EventType} to {HandlersCount} handlers in {TotalMs} ms",
                typeof(TNotification).FullName,
                handlers.Count,
                totalSw.ElapsedMilliseconds);
        }
    }
}

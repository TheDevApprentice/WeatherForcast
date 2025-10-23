using System;
using System.Collections.Generic;
using System.Linq;
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

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling event {EventType} with {Handler}", typeof(TNotification).FullName, handler.GetType().FullName);
                    // Ne pas throw pour ne pas bloquer les autres handlers (comportement similaire à MediatR dans vos handlers)
                }
            }
        }
    }
}

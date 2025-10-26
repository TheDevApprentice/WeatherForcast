using System;
using System.Linq;
using System.Reflection;
using domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace shared.Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddScoped<IPublisher, EventPublisher>();

            foreach (var assembly in assemblies.Distinct())
            {
                var handlerRegistrations =
                    from type in assembly.GetTypes()
                    where !type.IsAbstract && !type.IsInterface
                    from itf in type.GetInterfaces()
                    where itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(INotificationHandler<>)
                    select new { HandlerType = type, ServiceType = itf };

                foreach (var reg in handlerRegistrations)
                {
                    services.AddScoped(reg.ServiceType, reg.HandlerType);
                }
            }

            return services;
        }
    }
}

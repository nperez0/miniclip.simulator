using Mediator;
using Miniclip.Core.Application.Behaviors;
using Miniclip.Core.ServiceDefaults.Behaviors;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class MediatorConfiguration
{
    public static IServiceCollection AddMediatorDependencies(this IServiceCollection services)
    {
        services.AddSingleton<INotificationPublisher, OrderedNotificationPublisher>();

        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStoreCommandBehavior<,>));

        return services;
    }
}

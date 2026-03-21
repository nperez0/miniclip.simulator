using Mediator;
using Miniclip.Core.Application.Behaviors;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class MediatorConfiguration
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddSingleton<INotificationPublisher, OrderedNotificationPublisher>();

        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ReadModelUnitOfWorkBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(DomainEventPublisherBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStoreCommandBehavior<,>));

        return services;
    }
}

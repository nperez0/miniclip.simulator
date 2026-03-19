using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Application.Behaviors;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class MediatorConfiguration
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddSingleton<INotificationPublisher, OrderedNotificationPublisher>();

        services.AddMediator();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CommandUnitOfWorkBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ReadModelUnitOfWorkBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(DomainEventPublisherBehavior<,>));

        return services;
    }
}

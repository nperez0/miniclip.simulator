using Mediator;
using Miniclip.Core.Application.Behaviors;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class MediatorConfiguration
{
    public static IServiceCollection AddMediatorDependencies(this IServiceCollection services)
    {
        services.AddSingleton<INotificationPublisher, OrderedNotificationPublisher>();
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

        return services;
    }
}

using Miniclip.Core.Application.Behaviors;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class MediatorConfiguration
{
    public static IServiceCollection AddMediatorDependencies(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Telemetry.EnableMetrics = true;
            options.Telemetry.EnableTracing = true;
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStoreCommandBehavior<,>));

        return services;
    }
}

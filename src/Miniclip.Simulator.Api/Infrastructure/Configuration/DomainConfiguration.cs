using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class DomainConfiguration
{
    public static IServiceCollection AddDomainDependencies(this IServiceCollection services)
    {
        services.AddScoped<IMatchSimulatorFactory, MatchSimulatorFactory>();
        services.AddScoped<IGroupSimulator, GroupSimulator>();
        services.AddScoped<IFixtureSchedulerFactory, FixtureSchedulerFactory>();
        services.AddScoped<IFixtureSchedulerService, FixtureSchedulerService>();

        return services;
    }
}

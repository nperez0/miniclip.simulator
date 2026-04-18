using Miniclip.Core.ReadModels.Projections.Configuration;
using Miniclip.Simulator.ReadModels.Projections;
using Miniclip.Simulator.ReadModels.Projections.Services;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class ProjectionsConfiguration
{
    public static IServiceCollection AddProjectionsDependencies(this IServiceCollection services)
    {
        services.AddScoped<IRecalculatePositionService, RecalculatePositionService>();
        services.AddProjectionHandlers(typeof(MatchResultProjection).Assembly);

        return services;
    }
}

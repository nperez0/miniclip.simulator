using Microsoft.EntityFrameworkCore;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Read = Miniclip.Simulator.ReadModels.Repositories.Read;
using ReadRepo = Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Read;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class ReadModelsConfiguration
{
    public static IServiceCollection AddReadModelsDbDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SimulatorRead");

        services.AddDbContext<SimulatorReadDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddScoped<Read.IGroupStandingsRepository, ReadRepo.GroupStandingsRepository>();
        services.AddScoped<Read.IMatchResultsRepository, ReadRepo.MatchResultsRepository>();

        return services;
    }
}


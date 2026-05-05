using Microsoft.EntityFrameworkCore;
using Miniclip.Core.ReadModels;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class ReadModelsConfiguration
{
    public static IServiceCollection AddReadModelsDbDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SimulatorRead");

        services.AddDbContext<SimulatorReadDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddScoped<IReadModelUnitOfWork, SimulatorReadModelUnitOfWork>();
        services.AddScoped<IGroupStandingsRepository, GroupStandingsRepository>();
        services.AddScoped<IMatchResultsRepository, MatchResultsRepository>();

        services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

        return services;
    }

    public static void InitializeDatabases(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var readContext = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        readContext.Database.Migrate();
    }
}

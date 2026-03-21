using Microsoft.EntityFrameworkCore;
using Miniclip.Core.ReadModels;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Read = Miniclip.Simulator.ReadModels.Repositories.Read;
using ReadRepo = Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Read;
using Write = Miniclip.Simulator.ReadModels.Repositories.Write;
using WriteRepo = Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class ReadModelsConfiguration
{
    public static IServiceCollection AddReadModelsDbDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SimulatorRead");

        services.AddDbContext<SimulatorReadDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // Team reference data — read from write DB (snapshot at group creation time)
        services.AddScoped<IReadModelUnitOfWork, SimulatorReadModelUnitOfWork>();

        // Read models repositories - Group Standings
        services.AddScoped<Read.IGroupStandingsRepository, ReadRepo.GroupStandingsRepository>();
        services.AddScoped<Write.IGroupStandingsRepository, WriteRepo.GroupStandingsRepository>();

        // Read models repositories - Match Results
        services.AddScoped<Read.IMatchResultsRepository, ReadRepo.MatchResultsRepository>();
        services.AddScoped<Write.IMatchResultsRepository, WriteRepo.MatchResultsRepository>();

        return services;
    }

    public static void InitializeDatabases(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var readContext = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        readContext.Database.Migrate();
    }
}

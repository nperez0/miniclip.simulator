using Microsoft.EntityFrameworkCore;
using Miniclip.Core.Domain;
using Miniclip.Core.EF;
using Miniclip.Core.ReadModels;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories;
using Miniclip.Simulator.Infrastructure.Write.Persistence;
using Miniclip.Simulator.Infrastructure.Write.Persistence.Repositories;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var writeConnectionString = configuration.GetConnectionString("SimulatorWrite");
        var readConnectionString = configuration.GetConnectionString("SimulatorRead");

        services.AddDbContext<SimulatorWriteDbContext>(options =>
            options.UseMySql(writeConnectionString, ServerVersion.AutoDetect(writeConnectionString)));

        services.AddDbContext<SimulatorReadDbContext>(options =>
            options.UseMySql(readConnectionString, ServerVersion.AutoDetect(readConnectionString)));

        // Domain repositories
        services.AddScoped<IRepository<Group>, GroupsRepository>();
        services.AddScoped<IRepository<Team>>(sp =>
            new Repository<Team>(sp.GetRequiredService<SimulatorWriteDbContext>()));

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp =>
            new SimulatorUnitOfWork(sp.GetRequiredService<SimulatorWriteDbContext>()));
        services.AddScoped<IReadModelUnitOfWork>(sp =>
            new SimulatorReadModelUnitOfWork(sp.GetRequiredService<SimulatorReadDbContext>()));

        // Read models repositories
        services.AddScoped<IRepository<GroupStandingsModel>>(sp =>
            new Repository<GroupStandingsModel>(sp.GetRequiredService<SimulatorReadDbContext>()));
        services.AddScoped<IGroupStandingsRepository, GroupStandingsRepository>();

        return services;
    }

    public static void InitializeDatabases(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var writeContext = scope.ServiceProvider.GetRequiredService<SimulatorWriteDbContext>();
        var readContext = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        writeContext.Database.Migrate();
        readContext.Database.Migrate();
    }
}

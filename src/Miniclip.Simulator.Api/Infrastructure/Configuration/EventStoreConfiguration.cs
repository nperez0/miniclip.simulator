using KurrentDB.Client;
using Miniclip.Core.Application.Publishers;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.EventSourcing.EventStoreDB.Configuration;
using Miniclip.Simulator.Api.Infrastructure.Seeding;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class EventStoreConfiguration
{
    public static IServiceCollection AddEventStoreDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventStore")!;
        var settings = KurrentDBClientSettings.Create(connectionString);

        services.AddSingleton(_ => new KurrentDBClient(settings));

        services.AddEventStoreInfrastructure();
        
        services.AddScoped<IAggregateRepository<Group>, AggregateRepository<Group>>();
        services.AddScoped<IAggregateRepository<Team>, AggregateRepository<Team>>();

        services.AddScoped<ICommittedEventPublisher, CommittedEventPublisher>();

        services.AddHostedService<TeamDataSeeder>();

        return services;
    }
}

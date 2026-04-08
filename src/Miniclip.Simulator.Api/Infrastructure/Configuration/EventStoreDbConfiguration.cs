using KurrentDB.Client;
using Miniclip.Core.Application.Serializers;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.EventSourcing.EventStoreDB;
using Miniclip.Simulator.Api.Infrastructure.Seeding;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class EventStoreDbConfiguration
{
    public static IServiceCollection AddEventStoreDbDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventStore")!;
        var settings = KurrentDBClientSettings.Create(connectionString);

        services.AddSingleton(_ => new KurrentDBClient(settings));
        services.AddSingleton<IEventSerializer, DomainEventJsonSerializer>();
        services.AddScoped<IEventStoreSession, EventStoreSession>();
        services.AddScoped(typeof(IEventStore<>), typeof(EventStoreDbEventStore<>));
        
        services.AddScoped<IAggregateRepository<Group>, AggregateRepository<Group>>();
        services.AddScoped<IAggregateRepository<Team>, AggregateRepository<Team>>();

        services.AddHostedService<TeamDataSeeder>();

        return services;
    }
}

using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Miniclip.Core.EventSourcing.EventStoreDB;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventStoreDbClient(
        this IServiceCollection services,
        string connectionString)
    {
        var settings = EventStoreClientSettings.Create(connectionString);

        services.AddSingleton(_ => new EventStoreClient(settings));
        services.AddSingleton<IEventSerializer, SystemTextJsonEventSerializer>();
        services.AddSingleton(typeof(IEventStore<>), typeof(EventStoreDbEventStore<>));

        return services;
    }
}

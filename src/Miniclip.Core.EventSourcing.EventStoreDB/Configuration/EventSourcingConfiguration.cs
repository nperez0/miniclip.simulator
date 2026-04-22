using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Reflection;
using Miniclip.Core.Domain;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.EventSourcing.EventStoreDB.Configuration;

public static class EventSourcingConfiguration
{
    public static IServiceCollection AddEventStoreInfrastructure(this IServiceCollection services)
    {
        AssemblyLoader.EnsureReferencedAssembliesLoaded();

        var types = AssemblyScanner
            .GetImplementationsOf<IDomainEvent>()
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

        services.AddSingleton<IDomainEventTypeRegistry>(new DomainEventTypeRegistry(types));

        services.AddSingleton<IDomainEventSerializer, DomainEventJsonSerializer>();

        services.AddScoped<IEventStoreSession, EventStoreSession>();
        services.AddScoped(typeof(IEventStore<>), typeof(EventStoreDbEventStore<>));

        return services;
    }
}

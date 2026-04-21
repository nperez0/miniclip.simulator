using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Domain;
using Miniclip.Core.Messaging;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.Application.Configuration;

public static class IntegrationEventMapperRegistryConfiguration
{
    public static IServiceCollection AddIntegrationEventMappers(this IServiceCollection services)
    {
        AssemblyLoader.EnsureReferencedAssembliesLoaded();

        var mapperInterface = typeof(IntegrationEvents.IIntegrationEventMapper<>);

        var entries = AssemblyScanner
            .GetClosedImplementationsOf(mapperInterface)
            .Select(x =>
            {
                var (mapperType, typeArgs) = x;
                var domainEventType = typeArgs[0];
                var mapperInstance = Activator.CreateInstance(mapperType)!;
                var method = mapperType.GetMethod(nameof(IntegrationEvents.IIntegrationEventMapper<IDomainEvent>.Map))!;

                return (domainEventType, (Func<IDomainEvent, IIntegrationEvent>)Invoke);

                IIntegrationEvent Invoke(IDomainEvent domainEvent) => (IIntegrationEvent)method.Invoke(mapperInstance, [domainEvent])!;
            });

        services.AddSingleton<IntegrationEvents.IIntegrationEventMapperRegistry>(
            new IntegrationEvents.IntegrationEventMapperRegistry(entries));

        return services;
    }
}

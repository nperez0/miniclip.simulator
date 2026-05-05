using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Application.IntegrationEvents;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.Application.Configuration;

public static class IntegrationEventMapperRegistryConfiguration
{
    public static IServiceCollection AddIntegrationEventMappers(this IServiceCollection services)
    {
        AssemblyLoader.EnsureReferencedAssembliesLoaded();

        var mapperInterface = typeof(IIntegrationEventMapper<,>);

        var entries = AssemblyScanner
            .GetClosedImplementationsOf(mapperInterface)
            .Select(x =>
            {
                var (mapperType, typeArgs) = x;
                var domainEventType = typeArgs[0];
                var integrationEventType = typeArgs[1];
                var integrationEventMessageTypeName = integrationEventType.GetMessageTypeName();
                var mapperInstance = Activator.CreateInstance(mapperType)!;
                var method = mapperType.GetMethod(nameof(IIntegrationEventMapper<,>.Map))!;

                return (domainEventType, integrationEventMessageTypeName, (Func<IDomainEvent, IIntegrationEvent>)Invoke);

                IIntegrationEvent Invoke(IDomainEvent domainEvent) => (IIntegrationEvent)method.Invoke(mapperInstance, [domainEvent])!;
            });

        var registry = new IntegrationEventMapperRegistry(entries);

        services.AddSingleton<IIntegrationEventMapperRegistry>(registry);

        return services;
    }
}
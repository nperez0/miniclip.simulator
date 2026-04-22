using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.Application.Configuration;

public static class IntegrationEventSerializerConfiguration
{
    public static IServiceCollection AddIntegrationEventSerializer(this IServiceCollection services)
    {
        AssemblyLoader.EnsureReferencedAssembliesLoaded();

        var types = AssemblyScanner
            .GetImplementationsOf<IIntegrationEvent>()
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

        services.AddSingleton<IMessageTypeRegistry>(new MessageTypeRegistry(types));

        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        return services;
    }
}

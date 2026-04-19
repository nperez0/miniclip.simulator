using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Domain;

namespace Miniclip.Core.Application.Configuration;

public static class MessageTypeRegistryConfiguration
{
    public static IServiceCollection AddMessageTypeRegistry(this IServiceCollection services)
    {
        var types = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t)
                     && t is { IsInterface: false, IsAbstract: false, IsGenericTypeDefinition: false })
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

        services.AddSingleton<IMessageTypeRegistry>(new MessageTypeRegistry(types));

        return services;
    }
}

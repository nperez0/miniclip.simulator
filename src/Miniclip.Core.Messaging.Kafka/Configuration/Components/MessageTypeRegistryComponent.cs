using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Components;

internal sealed class MessageTypeRegistryComponent : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        var types = AssemblyScanner
            .GetImplementationsOf<IIntegrationEvent>()
            .ToDictionary(
                t => t.GetMessageTypeName(),
                StringComparer.Ordinal);

        services.TryAddSingleton<IMessageTypeRegistry>(new MessageTypeRegistry(types));
    }
}

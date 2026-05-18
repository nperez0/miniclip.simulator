using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Components;

internal sealed class JsonDeserializerComponent : IMessagingComponent
{
    public void Register(IServiceCollection services)
        => services.TryAddSingleton<IMessageDeserializer, JsonMessageSerializer>();
}

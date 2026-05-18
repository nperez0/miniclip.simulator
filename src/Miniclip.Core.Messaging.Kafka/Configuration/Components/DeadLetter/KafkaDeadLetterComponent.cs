using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Configuration;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Components.DeadLetter;

internal sealed class KafkaDeadLetterComponent(string bootstrapServers) : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        new KafkaProducerComponent(bootstrapServers).Register(services);
        services.AddSingleton<IDeadLetterHandler, KafkaDeadLetterHandler>();
    }
}

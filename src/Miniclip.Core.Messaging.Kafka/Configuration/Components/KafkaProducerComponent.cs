using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Components;

internal sealed class KafkaProducerComponent(string bootstrapServers) : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        services.TryAddSingleton(new InstrumentedProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = bootstrapServers }));

        services.TryAddSingleton<IProducer<string, string>>(
            sp => sp.GetRequiredService<InstrumentedProducerBuilder<string, string>>().Build());
    }
}

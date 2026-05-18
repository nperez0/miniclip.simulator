using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Kafka.Configuration.Builders;
using Miniclip.Core.Messaging.Outbound;
using Miniclip.Core.Messaging.Pipeline.Outbound;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Components;

internal sealed class KafkaEventBusComponent(string bootstrapServers, OutboundTopicMappingBuilder mappingBuilder) : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        new KafkaProducerComponent(bootstrapServers).Register(services);

        var topicMap = mappingBuilder.Build();

        services.AddSingleton<IOutboundTopicRegistry>(new OutboundTopicRegistry(topicMap));
        services.AddSingleton<IDestinationResolver, KafkaDestinationResolver>();
        services.AddScoped<IEventDispatcher, KafkaEventDispatcher>();
        services.AddScoped<IEventBus, OutboundPipeline>();
    }
}

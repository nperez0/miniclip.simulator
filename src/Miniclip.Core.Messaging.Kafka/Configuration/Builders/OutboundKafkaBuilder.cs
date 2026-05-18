using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Kafka.Configuration.Components;
using Miniclip.Core.Messaging.Pipeline.Configuration.Builders;
using Miniclip.Core.Messaging.Pipeline.Configuration.Components;
using Miniclip.Core.Messaging.Pipeline.Outbound.Middleware;
using IOutboundMiddleware = Miniclip.Core.Messaging.Outbound.IOutboundMiddleware;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Builders;

public sealed class OutboundKafkaBuilder
{
    private readonly string bootstrapServers;
    private readonly List<Action<OutboundTopicMappingBuilder>> mappings = [];

    private IMessagingComponent serializerSlot = new JsonSerializerComponent();

    private readonly MiddlewareBuilder<IOutboundMiddleware> middlewareChain =
        new MiddlewareBuilder<IOutboundMiddleware>()
            .Add<PropagationEnrichmentMiddleware>()
            .Add<OutboundTracingMiddleware>();

    internal OutboundKafkaBuilder(string bootstrapServers)
    {
        this.bootstrapServers = bootstrapServers;
    }

    public OutboundKafkaBuilder ConfigureMiddleware(Action<MiddlewareBuilder<IOutboundMiddleware>> configure)
    {
        configure(middlewareChain);
        return this;
    }

    public OutboundKafkaBuilder UseSerializer(IMessagingComponent component)
    {
        serializerSlot = component;
        return this;
    }

    public OutboundKafkaBuilder UseJsonSerializer()
    {
        serializerSlot = new JsonSerializerComponent();
        return this;
    }

    public OutboundKafkaBuilder MapTopic<TMessage>(string topic) where TMessage : IIntegrationEvent
    {
        mappings.Add(b => b.MapTopic<TMessage>(topic));
        return this;
    }

    internal IReadOnlyList<IMessagingComponent> BuildComponents()
    {
        var topicMappingBuilder = new OutboundTopicMappingBuilder();
        foreach (var mapping in mappings)
            mapping(topicMappingBuilder);

        var chain = middlewareChain.Build();

        return
        [
            new OutboundPipelineComponent(chain.ToArray()),
            serializerSlot,
            new KafkaEventBusComponent(bootstrapServers, topicMappingBuilder),
        ];
    }
}

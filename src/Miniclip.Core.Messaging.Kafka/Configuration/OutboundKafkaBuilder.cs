using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Outbound;

namespace Miniclip.Core.Messaging.Kafka.Configuration;

public sealed class OutboundKafkaBuilder
{
    private readonly List<Action<OutboundTopicMappingBuilder>> mappings = [];

    public OutboundKafkaBuilder MapTopic<TMessage>(string topic) where TMessage : IIntegrationEvent
    {
        mappings.Add(b => b.MapTopic<TMessage>(topic));
        return this;
    }

    internal void Build(IServiceCollection services, string bootstrapServers)
    {
        services.AddOutboundKafkaInfrastructure(bootstrapServers, topics =>
        {
            foreach (var mapping in mappings)
                mapping(topics);
        });
    }
}
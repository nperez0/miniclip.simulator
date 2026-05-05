using Miniclip.Core.Messaging.Outbound;

namespace Miniclip.Core.Messaging.Kafka;

public sealed class KafkaDestinationResolver(IOutboundTopicRegistry topicRegistry) : IDestinationResolver
{
    public string Resolve(OutboundEnvelope envelope)
    {
        if (!envelope.Headers.TryGetValue(MessageHeaders.MessageType, out var messageTypeName)
            || messageTypeName.IsNullOrEmpty())
        {
            throw new InvalidOperationException(
                $"Outbound envelope is missing the '{MessageHeaders.MessageType}' header. " +
                $"Ensure the outbound pipeline stamps this header before dispatching.");
        }

        return topicRegistry.Resolve(messageTypeName);
    }
}
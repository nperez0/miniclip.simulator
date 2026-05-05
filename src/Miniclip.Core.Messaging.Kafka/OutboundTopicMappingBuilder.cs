namespace Miniclip.Core.Messaging.Kafka;

public sealed class OutboundTopicMappingBuilder
{
    private readonly Dictionary<string, string> topicMap = [];

    public OutboundTopicMappingBuilder MapTopic<TMessage>(string topic) where TMessage : IIntegrationEvent
    {
        var messageTypeName = typeof(TMessage).GetMessageTypeName();

        if (!topicMap.TryAdd(messageTypeName, topic))
        {
            throw new InvalidOperationException(
                $"A topic mapping for message type '{messageTypeName}' is already registered. " +
                $"Each message type can only map to one Kafka topic.");
        }

        return this;
    }

    internal Dictionary<string, string> Build() => topicMap;
}

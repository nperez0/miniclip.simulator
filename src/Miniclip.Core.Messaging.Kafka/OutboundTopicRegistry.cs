namespace Miniclip.Core.Messaging.Kafka;

internal sealed class OutboundTopicRegistry(Dictionary<string, string> topicMap) : IOutboundTopicRegistry
{
    public string Resolve(string messageTypeName)
    {
        if (!topicMap.TryGetValue(messageTypeName, out var topic))
        {
            throw new InvalidOperationException(
                $"No Kafka topic mapping registered for message type '{messageTypeName}'. " +
                $"Call MapTopic<TMessage>(...) during Kafka outbound configuration for this type.");
        }

        return topic;
    }

    public bool Contains(string messageTypeName) => topicMap.ContainsKey(messageTypeName);
}
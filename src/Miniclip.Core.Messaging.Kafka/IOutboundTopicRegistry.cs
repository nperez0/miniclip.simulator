namespace Miniclip.Core.Messaging.Kafka;

public interface IOutboundTopicRegistry
{
    string Resolve(string messageTypeName);
    bool Contains(string messageTypeName);
}
namespace Miniclip.Core.Messaging.Inbound;

public interface IMessageEnvelope
{
    string MessageId { get; }
    string MessageType { get; }
    string Payload { get; }
    IReadOnlyDictionary<string, string> Headers { get; }
}

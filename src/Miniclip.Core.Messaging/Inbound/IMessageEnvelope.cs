namespace Miniclip.Core.Messaging.Inbound;

public interface IMessageEnvelope
{
    string MessageId { get; }

    string MessageType { get; }

    ReadOnlyMemory<byte> Payload { get; }

    IReadOnlyDictionary<string, string> Headers { get; }
}

namespace Miniclip.Core.Messaging;

public interface IMessageEnvelope
{
    string MessageId { get; }

    string MessageType { get; }

    ReadOnlyMemory<byte> Payload { get; }

    IReadOnlyDictionary<string, string> Headers { get; }
}

namespace Miniclip.Core.Messaging.Inbound;

public sealed record MessageEnvelope(
    string MessageId,
    string MessageType,
    ReadOnlyMemory<byte> Payload,
    IReadOnlyDictionary<string, string> Headers) : IMessageEnvelope;

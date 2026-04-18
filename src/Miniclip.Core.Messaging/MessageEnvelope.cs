namespace Miniclip.Core.Messaging;

public sealed record MessageEnvelope(
    string MessageId,
    string MessageType,
    ReadOnlyMemory<byte> Payload,
    IReadOnlyDictionary<string, string> Headers) : IMessageEnvelope;

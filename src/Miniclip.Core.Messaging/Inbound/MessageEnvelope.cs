namespace Miniclip.Core.Messaging.Inbound;

public sealed record MessageEnvelope(
    string MessageId,
    string MessageType,
    string Payload,
    IReadOnlyDictionary<string, string> Headers) : IMessageEnvelope;

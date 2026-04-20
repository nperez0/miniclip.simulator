namespace Miniclip.Core.Messaging.Outbound;

public sealed class OutboundEnvelope(object @event, string? messageGroupId = null, Dictionary<string, string?>? headers = null)
{
    public object Event { get; } = @event;
    public string EventType => Event.GetType().FullName!;
    public string MessageGroupId { get; } = messageGroupId ?? Guid.NewGuid().ToString();
    public Dictionary<string, string?> Headers { get; } = headers ?? new Dictionary<string, string?>();
}


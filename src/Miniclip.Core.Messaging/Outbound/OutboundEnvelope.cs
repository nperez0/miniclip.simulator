namespace Miniclip.Core.Messaging.Outbound;

public sealed class OutboundEnvelope(object @event, Dictionary<string, string> headers)
{
    public object Event { get; } = @event;

    public Dictionary<string, string> Headers { get; } = headers;
}


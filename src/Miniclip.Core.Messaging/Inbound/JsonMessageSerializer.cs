using System.Text.Json;

namespace Miniclip.Core.Messaging.Inbound;

public sealed class JsonMessageSerializer(IMessageTypeRegistry registry) : IMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public byte[] Serialize(object @event)
        => JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), Options);

    public object Deserialize(string messageType, ReadOnlyMemory<byte> payload)
    {
        var type = registry.Resolve(messageType)
            ?? throw new InvalidOperationException($"Unknown message type '{messageType}'.");

        return JsonSerializer.Deserialize(payload.Span, type, Options)!;
    }
}

using System.Text.Json;

namespace Miniclip.Core.Messaging;

public sealed class JsonMessageSerializer(IMessageTypeRegistry registry) : IMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public string Serialize(object @event)
        => JsonSerializer.Serialize(@event, @event.GetType(), Options);

    public object Deserialize(string messageType, string payload)
    {
        var type = registry.Resolve(messageType)
            ?? throw new InvalidOperationException($"Unknown message type '{messageType}'.");

        return JsonSerializer.Deserialize(payload, type, Options)!;
    }
}

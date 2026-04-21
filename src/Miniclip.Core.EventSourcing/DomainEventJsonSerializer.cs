using System.Text.Json;
using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public sealed class DomainEventJsonSerializer(IDomainEventTypeRegistry registry) : IDomainEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public (string EventType, byte[] Data) Serialize(IDomainEvent @event)
    {
        var eventType = @event.GetType().Name;
        var data = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), Options);
        return (eventType, data);
    }

    public IDomainEvent Deserialize(string eventType, byte[] data)
    {
        var type = registry.Resolve(eventType)
            ?? throw new InvalidOperationException($"Unknown event type '{eventType}'.");
        return (IDomainEvent)JsonSerializer.Deserialize(data, type, Options)!;
    }
}
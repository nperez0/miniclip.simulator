using Miniclip.Core.Domain;
using System.Text.Json;

namespace Miniclip.Core.EventSourcing.EventStoreDB;

public sealed class SystemTextJsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
    private readonly IReadOnlyDictionary<string, Type> eventTypes;

    public SystemTextJsonEventSerializer()
    {
        eventTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public (string EventType, byte[] Data) Serialize(IDomainEvent @event)
    {
        var eventType = @event.GetType().Name;
        var data = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), options);

        return (eventType, data);
    }

    public IDomainEvent Deserialize(string eventType, byte[] data)
    {
        if (!eventTypes.TryGetValue(eventType, out var type))
            throw new InvalidOperationException($"Unknown event type '{eventType}'.");

        return (IDomainEvent)JsonSerializer.Deserialize(data, type, options)!;
    }
}

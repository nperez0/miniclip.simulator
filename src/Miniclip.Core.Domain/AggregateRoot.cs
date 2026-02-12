using System.Text.Json.Serialization;

namespace Miniclip.Core.Domain;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }

    public long Version { get; protected set; }

    [JsonIgnore]
    private readonly Queue<object> uncommittedEvents = new();

    public object[] DequeueUncommittedEvents()
    {
        var events = uncommittedEvents.ToArray();

        uncommittedEvents.Clear();

        return events;
    }

    protected void Enqueue(object @event)
    {
        uncommittedEvents.Enqueue(@event);
        Version++;
    }
}

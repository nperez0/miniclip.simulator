using System.Text.Json.Serialization;

namespace Miniclip.Core.Domain;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }

    public long Version { get; protected set; } = -1;

    [JsonIgnore]
    private readonly Queue<IDomainEvent> uncommittedEvents = new();

    public IDomainEvent[] DequeueUncommittedEvents()
    {
        var events = uncommittedEvents.ToArray();

        uncommittedEvents.Clear();

        return events;
    }

    protected void Enqueue(IDomainEvent @event)
    {
        uncommittedEvents.Enqueue(@event);
    }

    public void ReplayEvent(IDomainEvent @event, long version)
    {
        Apply(@event);
        Version = version;
    }

    protected virtual void Apply(IDomainEvent @event)
    {
    }
}

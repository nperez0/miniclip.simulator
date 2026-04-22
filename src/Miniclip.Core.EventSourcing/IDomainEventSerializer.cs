namespace Miniclip.Core.EventSourcing;

public interface IDomainEventSerializer
{
    (string EventType, byte[] Data) Serialize(IDomainEvent @event);

    IDomainEvent Deserialize(string eventType, byte[] data);
}

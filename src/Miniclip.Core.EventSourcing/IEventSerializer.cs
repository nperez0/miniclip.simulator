using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public interface IEventSerializer
{
    (string EventType, byte[] Data) Serialize(IDomainEvent @event);

    IDomainEvent Deserialize(string eventType, byte[] data);
}

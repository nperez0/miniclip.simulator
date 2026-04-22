
namespace Miniclip.Simulator.Domain.Aggregates.Groups.Events;

public record GroupCreated(Guid GroupId, string Name, int Capacity) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => GroupId;
}


namespace Miniclip.Simulator.Domain.Aggregates.Teams.Events;

public record TeamRegistered(Guid TeamId, string Name, int Strength) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => TeamId;
}

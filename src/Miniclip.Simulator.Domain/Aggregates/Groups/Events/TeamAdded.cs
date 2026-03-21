using Miniclip.Core.Domain;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Events;

public record TeamAdded(Guid GroupId, Guid TeamId, string Name, int Strength) : IDomainEvent;

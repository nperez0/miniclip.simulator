using Miniclip.Core.Domain;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Events;

public record GroupCreated(Guid GroupId, string Name, int Capacity) : IDomainEvent;

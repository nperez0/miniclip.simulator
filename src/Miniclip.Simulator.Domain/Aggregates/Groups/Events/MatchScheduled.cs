using Miniclip.Core.Domain;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Events;

public record MatchScheduled(Guid GroupId, Guid MatchId, Guid HomeTeamId, Guid AwayTeamId, int Round) : IDomainEvent;

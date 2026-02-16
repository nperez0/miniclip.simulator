using Miniclip.Core.Domain;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Events;

public record MatchPlayed(
    Guid GroupId,
    string GroupName,
    Guid MatchId,
    Guid HomeTeamId,
    string HomeTeamName,
    int HomeTeamStrength,
    int HomeScore,
    Guid AwayTeamId,
    string AwayTeamName,
    int AwayTeamStrength,
    int AwayScore,
    int Round
) : IDomainEvent;

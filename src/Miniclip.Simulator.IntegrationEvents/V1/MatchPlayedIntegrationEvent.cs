
namespace Miniclip.Simulator.IntegrationEvents.V1;

public sealed record MatchPlayedIntegrationEvent(
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
) : IIntegrationEvent;

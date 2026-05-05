using Miniclip.Core.Application.IntegrationEvents;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;

namespace Miniclip.Simulator.IntegrationEvents.V1;

public sealed class MatchPlayedIntegrationEventMapper : IIntegrationEventMapper<MatchPlayed, MatchPlayedIntegrationEvent>
{
    public MatchPlayedIntegrationEvent Map(MatchPlayed domainEvent) =>
        new(
            GroupId: domainEvent.GroupId,
            GroupName: domainEvent.GroupName,
            MatchId: domainEvent.MatchId,
            HomeTeamId: domainEvent.HomeTeamId,
            HomeTeamName: domainEvent.HomeTeamName,
            HomeTeamStrength: domainEvent.HomeTeamStrength,
            HomeScore: domainEvent.HomeScore,
            AwayTeamId: domainEvent.AwayTeamId,
            AwayTeamName: domainEvent.AwayTeamName,
            AwayTeamStrength: domainEvent.AwayTeamStrength,
            AwayScore: domainEvent.AwayScore,
            Round: domainEvent.Round);
}
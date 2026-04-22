using Miniclip.Core.ReadModels.Projections.Attributes;
using Miniclip.Simulator.IntegrationEvents.V1;

namespace Miniclip.Simulator.ReadModels.Projections;

[HandlerHighPriority(1)]
public class MatchResultProjection(IMatchResultsRepository repository)
    : IProjectionHandler<MatchPlayedIntegrationEvent>
{
    public ValueTask HandleAsync(MatchPlayedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        var matchResult = new MatchResultModel
        {
            Id = Guid.NewGuid(),
            GroupId = @event.GroupId,
            GroupName = @event.GroupName,
            MatchId = @event.MatchId,
            Round = @event.Round,
            IsPlayed = true,
            HomeTeamId = @event.HomeTeamId,
            HomeTeamName = @event.HomeTeamName,
            HomeScore = @event.HomeScore,
            AwayTeamId = @event.AwayTeamId,
            AwayTeamName = @event.AwayTeamName,
            AwayScore = @event.AwayScore,
            PlayedAt = DateTime.UtcNow
        };

        repository.Add(matchResult);

        return ValueTask.CompletedTask;
    }
}

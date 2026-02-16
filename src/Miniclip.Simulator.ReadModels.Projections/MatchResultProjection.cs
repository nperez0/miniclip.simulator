using MediatR;
using Miniclip.Core.ReadModels.Projections.Attributes;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections;

[HandlerPriority(1)]
public class MatchResultProjection(IMatchResultsRepository repository) 
    : INotificationHandler<MatchPlayed>
{
    public Task Handle(MatchPlayed notification, CancellationToken cancellationToken)
    {
        var matchResult = new MatchResultModel
        {
            Id = Guid.NewGuid(),
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            MatchId = notification.MatchId,
            Round = notification.Round,
            IsPlayed = true,
            HomeTeamId = notification.HomeTeamId,
            HomeTeamName = notification.HomeTeamName,
            HomeScore = notification.HomeScore,
            AwayTeamId = notification.AwayTeamId,
            AwayTeamName = notification.AwayTeamName,
            AwayScore = notification.AwayScore,
            PlayedAt = DateTime.UtcNow
        };

        repository.Add(matchResult);

        return Task.CompletedTask;
    }
}

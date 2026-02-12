using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public class FixtureSchedulerService : IFixtureSchedulerService
{
    public Result GenerateFixtures(Group group)
    {
        if (group.Teams.Count < group.Capacity)
            return Result.Failure(GroupGenerateFixturesException.InvalidTeamCount(group.Capacity, group.Teams.Count));

        foreach (var match in RoundRobinScheduler.GenerateSchedule(group.Teams, group.Capacity))
        {
            var matchResult = group.AddMatch(Guid.NewGuid(), match.HomeTeam, match.AwayTeam, match.Round);

            if (matchResult.IsFailure)
                return matchResult;
        }

        return Result.Success();
    }
}

using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

public class FixtureSchedulerService : IFixtureSchedulerService
{
    private readonly IFixtureSchedulerFactory fixtureSchedulerFactory;

    public FixtureSchedulerService(IFixtureSchedulerFactory fixtureSchedulerFactory)
    {
        this.fixtureSchedulerFactory = fixtureSchedulerFactory;
    }

    public Result GenerateFixtures(Group group)
    {
        if (group.Teams.Count < group.Capacity)
            return Result.Failure(GroupGenerateFixturesException.InvalidTeamCount(group.Capacity, group.Teams.Count));

        var fixtureScheduler = fixtureSchedulerFactory.Create(group);

        foreach (var match in fixtureScheduler.GenerateSchedule())
        {
            var matchResult = group.AddMatch(Guid.NewGuid(), match.HomeTeam, match.AwayTeam, match.Round);

            if (matchResult.IsFailure)
                return matchResult;
        }

        return Result.Success();
    }
}

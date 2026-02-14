using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingSchedule;

public class WhenGeneratingSchedule : TestBase<RoundRobinScheduler>
{
    protected int Capacity { get; set; }

    protected List<Team> Teams { get; set; } = [];

    protected IEnumerable<(Team HomeTeam, Team AwayTeam, int Round)>? Schedule { get; set; }

    protected override RoundRobinScheduler CreateSystemUnderTest()
        => new(Teams, Capacity);

    protected override void When()
    {
        Schedule = Sut!.GenerateSchedule();
    }
}

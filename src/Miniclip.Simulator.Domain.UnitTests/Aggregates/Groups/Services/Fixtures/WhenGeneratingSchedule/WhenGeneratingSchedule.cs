using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingSchedule;

public class WhenGeneratingSchedule : TestBase<RoundRobinScheduler>
{
    protected int Capacity { get; set; }

    protected TeamInfo[] Teams { get; set; } = [];

    protected IEnumerable<(TeamInfo HomeTeam, TeamInfo AwayTeam, int Round)>? Schedule { get; set; }

    protected override RoundRobinScheduler CreateSystemUnderTest()
        => new(Teams, Capacity);

    protected override void When()
    {
        Schedule = Sut!.GenerateSchedule();
    }
}

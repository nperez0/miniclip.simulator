using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenSimulatingResult;

public class WhenSimulatingResult : TestBase<Match>
{
    protected Match? Match { get; set; }

    protected Result? Result { get; set; }

    protected int HomeScore { get; set; }

    protected int AwayScore { get; set; }

    protected override Match CreateSystemUnderTest()
        => null!;

    protected override void When()
    {
        Result = Match!.SimulateResult(HomeScore, AwayScore);
    }

    protected void GivenMatchWithTeams()
    {
        var homeTeam = TeamMother.Default();
        var awayTeam = TeamMother.Default();

        Match = Match.Create(Guid.NewGuid(), homeTeam, awayTeam, 1).Value!;
    }
}

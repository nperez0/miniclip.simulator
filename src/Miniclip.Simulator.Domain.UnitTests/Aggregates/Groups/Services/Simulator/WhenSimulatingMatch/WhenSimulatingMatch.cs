using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WhenSimulatingMatch : TestBase<MatchSimulator>
{
    protected int HomeTeamStrength { get; set; }
    protected int AwayTeamStrength { get; set; }

    protected int HomeScore { get; set; }

    protected int AwayScore { get; set; }

    protected override void When()
    {
        (HomeScore, AwayScore) = Sut!.SimulateMatch(HomeTeamStrength, AwayTeamStrength);
    }
}

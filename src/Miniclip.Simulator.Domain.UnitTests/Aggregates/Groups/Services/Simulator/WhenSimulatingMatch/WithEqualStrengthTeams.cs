using Shouldly;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WithEqualStrengthTeams : WhenSimulatingMatch
{
    protected override void Given()
    {
        HomeTeamStrength = 50;
        AwayTeamStrength = 50;
    }

    [Test]
    public void ShouldReturnNonNegativeScores()
    {
        HomeScore.ShouldBeGreaterThanOrEqualTo(0);
        AwayScore.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void ShouldReturnReasonableScores()
    {
        // Football scores rarely exceed 10 goals
        HomeScore.ShouldBeLessThanOrEqualTo(10);
        AwayScore.ShouldBeLessThanOrEqualTo(10);
    }
}

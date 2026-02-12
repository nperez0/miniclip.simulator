using FluentAssertions;
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
        HomeScore.Should().BeGreaterThanOrEqualTo(0);
        AwayScore.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void ShouldReturnReasonableScores()
    {
        // Football scores rarely exceed 10 goals
        HomeScore.Should().BeLessThanOrEqualTo(10);
        AwayScore.Should().BeLessThanOrEqualTo(10);
    }
}

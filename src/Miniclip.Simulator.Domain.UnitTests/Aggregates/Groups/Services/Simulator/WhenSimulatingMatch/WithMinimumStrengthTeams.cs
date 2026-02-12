using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WithMinimumStrengthTeams : WhenSimulatingMatch
{
    protected override void Given()
    {
        HomeTeamStrength = 0;
        AwayTeamStrength = 0;
    }

    [Test]
    public void ShouldProduceLowScores()
    {
        // Very weak teams should produce very low scores
        var totalGoals = HomeScore + AwayScore;
        totalGoals.Should().BeLessThanOrEqualTo(2);
    }

    [Test]
    public void ShouldNotProduceNegativeScores()
    {
        HomeScore.Should().BeGreaterThanOrEqualTo(0);
        AwayScore.Should().BeGreaterThanOrEqualTo(0);
    }
}

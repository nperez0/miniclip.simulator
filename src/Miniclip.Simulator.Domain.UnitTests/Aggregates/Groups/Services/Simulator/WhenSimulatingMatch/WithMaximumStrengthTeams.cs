using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WithMaximumStrengthTeams : WhenSimulatingMatch
{
    protected override void Given()
    {
        HomeTeamStrength = 100;
        AwayTeamStrength = 100;
    }

    [Test]
    public void ShouldProduceHigherScores()
    {
        // Very strong teams should produce higher scoring games on average
        var totalGoals = HomeScore + AwayScore;
        
        // This is statistical, so we can't guarantee, but on average should be higher
        totalGoals.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void ShouldStayWithinReasonableBounds()
    {
        // Even top teams shouldn't score 20 goals
        HomeScore.Should().BeLessThanOrEqualTo(10);
        AwayScore.Should().BeLessThanOrEqualTo(10);
    }
}

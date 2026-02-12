using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WithHomeAdvantage : WhenSimulatingMatch
{
    private const int Iterations = 1000;
    private int homeWins;
    private int awayWins;
    private int draws;

    protected override void Given()
    {
        // Equal strength teams to test home advantage effect
        HomeTeamStrength = 50;
        AwayTeamStrength = 50;
    }

    protected override void When()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var (home, away) = Sut!.SimulateMatch(HomeTeamStrength, AwayTeamStrength);

            if (home > away) homeWins++;
            else if (away > home) awayWins++;
            else draws++;
        }
    }

    [Test]
    public void ShouldGiveHomeTeamAdvantage()
    {
        // With equal strength, home team should win more due to home advantage
        homeWins.Should().BeGreaterThan(awayWins);
    }

    [Test]
    public void ShouldHaveReasonableHomeAdvantageEffect()
    {
        var homeWinPercentage = (double)homeWins / Iterations;
        
        // Home advantage should be noticeable but not overwhelming
        homeWinPercentage.Should().BeInRange(0.40, 0.60);
    }
}

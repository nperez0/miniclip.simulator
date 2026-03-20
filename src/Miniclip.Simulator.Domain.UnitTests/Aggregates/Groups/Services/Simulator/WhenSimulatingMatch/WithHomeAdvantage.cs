using Shouldly;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

[Category("Statistical")]
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
        homeWins.ShouldBeGreaterThan(awayWins);
    }

    [Test]
    public void ShouldHaveReasonableHomeAdvantageEffect()
    {
        var homeWinPercentage = (double)homeWins / Iterations;
        
        // Home advantage should be noticeable but not overwhelming
        homeWinPercentage.ShouldBeInRange(0.40, 0.60);
    }
}

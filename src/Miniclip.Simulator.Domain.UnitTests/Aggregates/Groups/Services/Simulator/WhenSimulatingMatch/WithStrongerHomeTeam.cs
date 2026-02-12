using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WithStrongerHomeTeam : WhenSimulatingMatch
{
    private const int Iterations = 1000;
    private int homeWins;
    private int awayWins;
    private int draws;

    protected override void Given()
    {
        HomeTeamStrength = 80;
        AwayTeamStrength = 40;
    }

    protected override void When()
    {
        // Simulate multiple matches to test statistical behavior
        for (int i = 0; i < Iterations; i++)
        {
            var (home, away) = Sut!.SimulateMatch(HomeTeamStrength, AwayTeamStrength);

            if (home > away) homeWins++;
            else if (away > home) awayWins++;
            else draws++;
        }
    }

    [Test]
    public void ShouldFavorStrongerTeam()
    {
        // Stronger home team should win more often
        homeWins.Should().BeGreaterThan(awayWins);
    }

    [Test]
    public void ShouldHaveReasonableWinPercentage()
    {
        var homeWinPercentage = (double)homeWins / Iterations;
        
        // With 80 vs 40 strength + home advantage, home should win at least 50% of the time
        homeWinPercentage.Should().BeGreaterThan(0.5);
    }
}

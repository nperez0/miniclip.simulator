using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WithStrongerAwayTeam : WhenSimulatingMatch
{
    private const int Iterations = 1000;
    private int homeWins;
    private int awayWins;
    private int draws;

    protected override void Given()
    {
        HomeTeamStrength = 40;
        AwayTeamStrength = 80;
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
    public void ShouldFavorStrongerAwayTeam()
    {
        awayWins.Should().BeGreaterThan(homeWins);
    }

    [Test]
    public void ShouldHaveReasonableWinPercentage()
    {
        var awayWinPercentage = (double)awayWins / Iterations;
        
        // Even with home advantage, stronger away team should win more
        awayWinPercentage.Should().BeGreaterThan(0.4);
    }
}

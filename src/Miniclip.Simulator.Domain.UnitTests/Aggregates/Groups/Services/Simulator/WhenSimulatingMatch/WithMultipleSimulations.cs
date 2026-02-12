using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingMatch;

public class WithMultipleSimulations : WhenSimulatingMatch
{
    private List<(int home, int away)> results = [];

    protected override void Given()
    {
        HomeTeamStrength = 60;
        AwayTeamStrength = 60;
    }

    protected override void When()
    {
        // Simulate same match multiple times
        for (int i = 0; i < 100; i++)
            results.Add(Sut!.SimulateMatch(HomeTeamStrength, AwayTeamStrength));
    }

    [Test]
    public void ShouldProduceDifferentResults()
    {
        // Results should vary (not all identical)
        results.Distinct().Count().Should().BeGreaterThan(10);
    }

    [Test]
    public void ShouldProduceRealisticScoreDistribution()
    {
        // Most games should be low-scoring (0-3 goals per team)
        var lowScoringGames = results.Count(r => r.home <= 3 && r.away <= 3);
        var percentage = (double)lowScoringGames / results.Count;
        
        percentage.Should().BeGreaterThan(0.6); // At least 60% should be low-scoring
    }
}

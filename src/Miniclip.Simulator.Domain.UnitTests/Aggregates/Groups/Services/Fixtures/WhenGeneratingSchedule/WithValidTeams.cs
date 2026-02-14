using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingSchedule;

[TestFixture(2)]
[TestFixture(3)]
[TestFixture(4)]
[TestFixture(5)]
[TestFixture(6)]
public class WithValidTeams(int capacity) : WhenGeneratingSchedule
{
    protected override void Given()
    {
        Capacity = capacity;
        Teams = Enumerable.Range(1, Capacity)
            .Select(x => Team.Create(Guid.NewGuid(), $"Team {x}", x * 10).Value!)
            .ToList();
    }

    [Test]
    public void ShouldGenerateCorrectNumberOfMatches()
    {
        // Each team plays every other team once: n * (n-1) / 2
        var expectedMatches = Capacity * (Capacity - 1) / 2;

        Schedule!.Should().HaveCount(expectedMatches);
    }

    [Test]
    public void ShouldHaveCorrectNumberOfRounds()
    {
        // For odd capacity, rounds = capacity (each team gets one bye)
        // For even capacity, rounds = capacity - 1
        var isOdd = Capacity % 2 == 1;
        var expectedRounds = isOdd ? Capacity : Capacity - 1;
        var actualRounds = Schedule!
            .Select(m => m.Round)
            .Distinct()
            .Count();

        actualRounds.Should().Be(expectedRounds);
    }

    [Test]
    public void ShouldNotContainDummyTeams()
    {
        Schedule!.Should().NotContain(m => m.HomeTeam == Team.Dummy || m.AwayTeam == Team.Dummy);
    }

    [Test]
    public void ShouldNotHaveTeamPlayingItself()
    {
        Schedule!.Should().OnlyContain(m => m.HomeTeam != m.AwayTeam);
    }

    [Test]
    public void ShouldHaveEachTeamPlayEveryOtherTeamOnce()
    {
        var teamPairings = new HashSet<string>();

        foreach (var match in Schedule!)
        {
            // Create a sorted pairing key to avoid duplicates (1-2 is same as 2-1)
            var pair = match.HomeTeam.Id < match.AwayTeam.Id
                ? $"{match.HomeTeam.Id}-{match.AwayTeam.Id}"
                : $"{match.AwayTeam.Id}-{match.HomeTeam.Id}";

            teamPairings.Add(pair);
        }

        // Each unique pairing should appear exactly once
        var expectedPairings = Capacity * (Capacity - 1) / 2;
        teamPairings.Should().HaveCount(expectedPairings);
    }

    [Test]
    public void ShouldDistributeMatchesEvenlyAcrossRounds()
    {
        var matchesPerRound = Schedule!.GroupBy(m => m.Round)
            .Select(g => g.Count())
            .ToList();

        var expectedMatchesPerRound = Capacity / 2;

        // All rounds should have the same number of matches
        matchesPerRound.Should().OnlyContain(count => count == expectedMatchesPerRound);
    }
}

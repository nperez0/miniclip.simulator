using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

[TestFixture(2)]
[TestFixture(3)]
[TestFixture(4)]
[TestFixture(5)]
[TestFixture(6)]
public class WithDifferentCapacities(int capacity) : WhenGeneratingFixtures
{
    protected override void Given()
    {
        Capacity = capacity;
    }

    [Test]
    public void ShouldGenerateCorrectNumberOfMatches()
    {
        // Each team plays every other team once: n * (n-1) / 2
        var expectedMatches = capacity * (capacity - 1) / 2;

        Group!.Matches.Should().HaveCount(expectedMatches);
    }

    [Test]
    public void ShouldHaveCorrectNumberOfRounds()
    {
        // For odd capacity, rounds = capacity (each team gets one bye)
        // For even capacity, rounds = capacity - 1
        var isOdd = capacity % 2 == 1;
        var expectedRounds = isOdd ? capacity : capacity - 1;
        var actualRounds = Group!
            .Matches
            .Select(m => m.Round)
            .Distinct()
            .Count();

        actualRounds.Should().Be(expectedRounds);
    }

    [Test]
    public void ShouldHaveEachTeamPlayOncePerRound()
    {
        var isOdd = capacity % 2 == 1;
        var matchesByRound = Group!.Matches.GroupBy(m => m.Round);

        foreach (var roundMatches in matchesByRound)
        {
            var teamsInRound = new List<Team>();

            foreach (var match in roundMatches)
            {
                teamsInRound.Add(match.HomeTeam);
                teamsInRound.Add(match.AwayTeam);
            }

            // Each team should appear exactly once per round
            teamsInRound.Should().OnlyHaveUniqueItems();
            
            // For even capacity, all teams play. For odd capacity, one team has a bye
            var expectedTeamsInRound = isOdd ? capacity - 1 : capacity;
            teamsInRound.Should().HaveCount(expectedTeamsInRound);
        }
    }

    [Test]
    public void ShouldHaveEachTeamPlayEveryOtherTeamOnce()
    {
        var teamPairings = new HashSet<string>();

        foreach (var match in Group!.Matches)
        {
            // Create a sorted pairing key to avoid duplicates (1-2 is same as 2-1)
            var pair = match.HomeTeam.Id < match.AwayTeam.Id
                ? $"{match.HomeTeam.Id}-{match.AwayTeam.Id}"
                : $"{match.AwayTeam.Id}-{match.HomeTeam.Id}";

            teamPairings.Add(pair);
        }

        // Each unique pairing should appear exactly once
        var expectedPairings = capacity * (capacity - 1) / 2;
        teamPairings.Should().HaveCount(expectedPairings);
    }

    [Test]
    public void ShouldHaveAllMatchesUnplayed()
    {
        Group!.Matches.Should().OnlyContain(m => m.IsPlayed == false);
    }

    [Test]
    public void ShouldHaveAllMatchesWithZeroScores()
    {
        Group!.Matches.Should().OnlyContain(m => m.HomeScore == 0 && m.AwayScore == 0);
    }
}

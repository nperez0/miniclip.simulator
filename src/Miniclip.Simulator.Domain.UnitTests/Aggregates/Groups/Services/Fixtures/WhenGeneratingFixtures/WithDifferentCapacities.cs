using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
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

        Group = Group.Create(Guid.NewGuid(), "Group A", Capacity).Value;

        Enumerable.Range(1, Capacity)
            .Each(x => Group!.AddTeam(Team.Create(Guid.NewGuid(), $"Team {x}", x).Value!));
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

    [Test]
    public void ShouldHaveBalancedHomeAwayGamesForEachTeam()
    {
        var homeGamesPerTeam = new Dictionary<Guid, int>();
        var awayGamesPerTeam = new Dictionary<Guid, int>();

        foreach (var match in Group!.Matches)
        {
            if (!homeGamesPerTeam.ContainsKey(match.HomeTeam.Id))
                homeGamesPerTeam[match.HomeTeam.Id] = 0;
            
            if (!awayGamesPerTeam.ContainsKey(match.AwayTeam.Id))
                awayGamesPerTeam[match.AwayTeam.Id] = 0;

            homeGamesPerTeam[match.HomeTeam.Id]++;
            awayGamesPerTeam[match.AwayTeam.Id]++;
        }

        // Each team plays (capacity - 1) games total
        var totalGamesPerTeam = capacity - 1;

        foreach (var team in Group.Teams)
        {
            var homeGames = homeGamesPerTeam.GetValueOrDefault(team.Id, 0);
            var awayGames = awayGamesPerTeam.GetValueOrDefault(team.Id, 0);

            // Total games should match
            (homeGames + awayGames).Should().Be(totalGamesPerTeam, 
                $"Team {team.Name} should play {totalGamesPerTeam} total games");

            // Difference between home/away should be at most 1
            Math.Abs(homeGames - awayGames).Should().BeLessThanOrEqualTo(1,
                $"Team {team.Name} should have balanced home ({homeGames}) and away ({awayGames}) games");
        }
    }
}

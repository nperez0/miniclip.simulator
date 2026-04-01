using Shouldly;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Queries.UnitTests.Groups.V1.WhenGettingGroupStandings;

public class WithExistingStandings : WhenGettingGroupStandings
{
    private Guid groupId;

    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        groupId = Guid.NewGuid();
        Query = new GroupStandingsQuery(groupId);

        var standings = new GroupStandingsModel[]
        {
            new() {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                GroupName = "Group A",
                Position = 1,
                TeamId = Guid.NewGuid(),
                TeamName = "Team 1",
                TeamStrength = 85,
                MatchesPlayed = 3,
                Wins = 3,
                Draws = 0,
                Losses = 0,
                GoalsFor = 8,
                GoalsAgainst = 2,
                GoalDifference = 6,
                Points = 9,
                QualifiesForKnockout = true
            },
            new() {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                GroupName = "Group A",
                Position = 2,
                TeamId = Guid.NewGuid(),
                TeamName = "Team 2",
                TeamStrength = 80,
                MatchesPlayed = 3,
                Wins = 2,
                Draws = 0,
                Losses = 1,
                GoalsFor = 6,
                GoalsAgainst = 4,
                GoalDifference = 2,
                Points = 6,
                QualifiesForKnockout = true
            },
            new() {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                GroupName = "Group A",
                Position = 3,
                TeamId = Guid.NewGuid(),
                TeamName = "Team 3",
                TeamStrength = 75,
                MatchesPlayed = 3,
                Wins = 1,
                Draws = 0,
                Losses = 2,
                GoalsFor = 4,
                GoalsAgainst = 6,
                GoalDifference = -2,
                Points = 3,
                QualifiesForKnockout = false
            }
        };

        var matchResults = new MatchResultModel[]
        {
            new() {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                MatchId = Guid.NewGuid(),
                Round = 1,
                HomeTeamName = "Team 1",
                HomeScore = 3,
                AwayTeamName = "Team 2",
                AwayScore = 1,
                IsPlayed = true,
                PlayedAt = DateTime.UtcNow.AddDays(-2)
            },
            new() {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                MatchId = Guid.NewGuid(),
                Round = 1,
                HomeTeamName = "Team 3",
                HomeScore = 2,
                AwayTeamName = "Team 1",
                AwayScore = 3,
                IsPlayed = true,
                PlayedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        StandingsRepository.GetStandingsByGroupIdAsync(groupId, CancellationToken.None)
            .ReturnsForAnyArgs(standings);

        MatchResultsRepository.GetMatchResultsByGroupIdAsync(groupId, CancellationToken.None)
            .ReturnsForAnyArgs(matchResults);
    }

    [Test]
    public void ShouldReturnGroupId()
    {
        Result.Value!.GroupId.ShouldBe(groupId);
    }

    [Test]
    public void ShouldReturnGroupName()
    {
        Result.Value!.GroupName.ShouldBe("Group A");
    }

    [Test]
    public void ShouldReturnAllStandings()
    {
        Result.Value!.Standings.Count().ShouldBe(3);
    }

    [Test]
    public void ShouldMapStandingsCorrectly()
    {
        var firstPlace = Result.Value!.Standings.First(s => s.Position == 1);
        firstPlace.TeamName.ShouldBe("Team 1");
        firstPlace.Points.ShouldBe(9);
        firstPlace.QualifiesForKnockout.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnAllMatchResults()
    {
        Result.Value!.MatchResults.Count().ShouldBe(2);
    }

    [Test]
    public void ShouldMapMatchResultsCorrectly()
    {
        var firstMatch = Result.Value!.MatchResults.First(m => m.Round == 1 && m.HomeTeamName == "Team 1");
        firstMatch.HomeScore.ShouldBe(3);
        firstMatch.AwayScore.ShouldBe(1);
        firstMatch.AwayTeamName.ShouldBe("Team 2");
    }

    [Test]
    public void ShouldReturnQualifiedTeams()
    {
        Result.Value!.QualifiedTeams.Count().ShouldBe(2);
        Result.Value!.QualifiedTeams.ShouldAllBe(t => t.QualifiesForKnockout);
    }
}

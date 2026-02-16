using FluentAssertions;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Queries.UnitTests.Groups.V1.WhenGettingGroupStandings;

public class WithExistingStandings : WhenGettingGroupStandings
{
    private Guid groupId;

    protected override void Given()
    {
        base.Given();

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

        StandingsRepository.GetStandingsByGroupIdAsync(groupId, default)
            .ReturnsForAnyArgs(standings);

        MatchResultsRepository.GetMatchResultsByGroupIdAsync(groupId, default)
            .ReturnsForAnyArgs(matchResults);
    }

    [Test]
    public void ShouldReturnGroupId()
    {
        Result.GroupId.Should().Be(groupId);
    }

    [Test]
    public void ShouldReturnGroupName()
    {
        Result.GroupName.Should().Be("Group A");
    }

    [Test]
    public void ShouldReturnAllStandings()
    {
        Result.Standings.Should().HaveCount(3);
    }

    [Test]
    public void ShouldMapStandingsCorrectly()
    {
        var firstPlace = Result.Standings.First(s => s.Position == 1);
        firstPlace.TeamName.Should().Be("Team 1");
        firstPlace.Points.Should().Be(9);
        firstPlace.QualifiesForKnockout.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnAllMatchResults()
    {
        Result.MatchResults.Should().HaveCount(2);
    }

    [Test]
    public void ShouldMapMatchResultsCorrectly()
    {
        var firstMatch = Result.MatchResults.First(m => m.Round == 1 && m.HomeTeamName == "Team 1");
        firstMatch.HomeScore.Should().Be(3);
        firstMatch.AwayScore.Should().Be(1);
        firstMatch.AwayTeamName.Should().Be("Team 2");
    }

    [Test]
    public void ShouldReturnQualifiedTeams()
    {
        Result.QualifiedTeams.Should().HaveCount(2);
        Result.QualifiedTeams.Should().OnlyContain(t => t.QualifiesForKnockout);
    }
}

using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingGroupStandings;

public class WithExistingStandings : WhenProjectingGroupStandings
{
    private Guid groupId;
    private Guid homeTeamId;
    private Guid awayTeamId;
    private GroupStandingsModel existingHomeStanding = null!;
    private GroupStandingsModel existingAwayStanding = null!;

    protected override void Given()
    {
        base.Given();

        groupId = Guid.NewGuid();
        homeTeamId = Guid.NewGuid();
        awayTeamId = Guid.NewGuid();

        existingHomeStanding = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            GroupName = "Group A",
            TeamId = homeTeamId,
            TeamName = "Home Team",
            TeamStrength = 80,
            MatchesPlayed = 1,
            Wins = 0,
            Draws = 1,
            Losses = 0,
            GoalsFor = 1,
            GoalsAgainst = 1,
            GoalDifference = 0,
            Points = 1
        };

        existingAwayStanding = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            GroupName = "Group A",
            TeamId = awayTeamId,
            TeamName = "Away Team",
            TeamStrength = 75,
            MatchesPlayed = 1,
            Wins = 1,
            Draws = 0,
            Losses = 0,
            GoalsFor = 2,
            GoalsAgainst = 0,
            GoalDifference = 2,
            Points = 3
        };

        Event = new MatchPlayed(
            GroupId: groupId,
            GroupName: "Group A",
            MatchId: Guid.NewGuid(),
            HomeTeamId: homeTeamId,
            HomeTeamName: "Home Team",
            HomeTeamStrength: 80,
            HomeScore: 3,
            AwayTeamId: awayTeamId,
            AwayTeamName: "Away Team",
            AwayTeamStrength: 75,
            AwayScore: 3,
            Round: 2
        );

        Repository
            .GetStandingsByGroupIdAsync(groupId, default)
            .ReturnsForAnyArgs([existingHomeStanding, existingAwayStanding]);
    }

    [Test]
    public void ShouldNotAddNewStandings()
    {
        Repository.DidNotReceive().Add(Arg.Any<GroupStandingsModel>());
    }

    [Test]
    public void ShouldUpdateHomeTeamStats()
    {
        existingHomeStanding.MatchesPlayed.Should().Be(2);
        existingHomeStanding.Wins.Should().Be(0);
        existingHomeStanding.Draws.Should().Be(2);
        existingHomeStanding.Losses.Should().Be(0);
        existingHomeStanding.GoalsFor.Should().Be(4);
        existingHomeStanding.GoalsAgainst.Should().Be(4);
        existingHomeStanding.GoalDifference.Should().Be(0);
        existingHomeStanding.Points.Should().Be(2);
    }

    [Test]
    public void ShouldUpdateAwayTeamStats()
    {
        existingAwayStanding.MatchesPlayed.Should().Be(2);
        existingAwayStanding.Wins.Should().Be(1);
        existingAwayStanding.Draws.Should().Be(1);
        existingAwayStanding.Losses.Should().Be(0);
        existingAwayStanding.GoalsFor.Should().Be(5);
        existingAwayStanding.GoalsAgainst.Should().Be(3);
        existingAwayStanding.GoalDifference.Should().Be(2);
        existingAwayStanding.Points.Should().Be(4);
    }

    [Test]
    public void ShouldCallRecalculatePositions()
    {
        RecalculatePositionService
            .Received(1)
            .RecalculatePositionsAsync(
                Arg.Is<IEnumerable<GroupStandingsModel>>(standings => standings.Count() == 2),
                groupId,
                default);
    }
}

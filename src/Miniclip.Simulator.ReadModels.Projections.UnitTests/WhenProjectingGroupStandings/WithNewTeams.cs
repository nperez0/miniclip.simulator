using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingGroupStandings;

public class WithNewTeams : WhenProjectingGroupStandings
{
    private Guid groupId;
    private Guid homeTeamId;
    private Guid awayTeamId;

    protected override void Given()
    {
        base.Given();

        groupId = Guid.NewGuid();
        homeTeamId = Guid.NewGuid();
        awayTeamId = Guid.NewGuid();

        Event = new MatchPlayed(
            GroupId: groupId,
            GroupName: "Group A",
            MatchId: Guid.NewGuid(),
            HomeTeamId: homeTeamId,
            HomeTeamName: "Home Team",
            HomeTeamStrength: 80,
            HomeScore: 2,
            AwayTeamId: awayTeamId,
            AwayTeamName: "Away Team",
            AwayTeamStrength: 75,
            AwayScore: 1,
            Round: 1
        );

        Repository.GetStandingsByGroupIdAsync(groupId, default)
            .ReturnsForAnyArgs([]);
    }

    [Test]
    public void ShouldAddBothTeamStandings()
    {
        Repository.Received(2).Add(Arg.Any<GroupStandingsModel>());
    }

    [Test]
    public void ShouldCreateHomeTeamStandingWithCorrectStats()
    {
        Repository.Received(1).Add(Arg.Is<GroupStandingsModel>(s =>
            s.TeamId == homeTeamId &&
            s.TeamName == "Home Team" &&
            s.TeamStrength == 80 &&
            s.MatchesPlayed == 1 &&
            s.Wins == 1 &&
            s.Draws == 0 &&
            s.Losses == 0 &&
            s.GoalsFor == 2 &&
            s.GoalsAgainst == 1 &&
            s.GoalDifference == 1 &&
            s.Points == 3
        ));
    }

    [Test]
    public void ShouldCreateAwayTeamStandingWithCorrectStats()
    {
        Repository.Received(1).Add(Arg.Is<GroupStandingsModel>(s =>
            s.TeamId == awayTeamId &&
            s.TeamName == "Away Team" &&
            s.TeamStrength == 75 &&
            s.MatchesPlayed == 1 &&
            s.Wins == 0 &&
            s.Draws == 0 &&
            s.Losses == 1 &&
            s.GoalsFor == 1 &&
            s.GoalsAgainst == 2 &&
            s.GoalDifference == -1 &&
            s.Points == 0
        ));
    }

    [Test]
    public void ShouldCallRecalculatePositions()
    {
        RecalculatePositionService.Received(1).RecalculatePositionsAsync(
            Arg.Is<IEnumerable<GroupStandingsModel>>(standings => standings.Count() == 2),
            groupId,
            default
        );
    }
}

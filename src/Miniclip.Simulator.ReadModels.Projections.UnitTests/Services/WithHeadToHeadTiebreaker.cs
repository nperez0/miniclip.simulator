using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.Services;

public class WithHeadToHeadTiebreaker : WhenRecalculatingPosition
{
    private Guid teamAId;
    private Guid teamBId;
    private GroupStandingsModel teamA = null!;
    private GroupStandingsModel teamB = null!;

    protected override Task GivenScenarioAsync()
    {
        GroupId = Guid.NewGuid();
        teamAId = Guid.NewGuid();
        teamBId = Guid.NewGuid();

        teamA = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            TeamId = teamAId,
            TeamName = "Team A",
            Points = 6,
            GoalDifference = 3,
            GoalsFor = 6,
            GoalsAgainst = 3
        };

        teamB = new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            TeamId = teamBId,
            TeamName = "Team B",
            Points = 6,
            GoalDifference = 3,
            GoalsFor = 6,
            GoalsAgainst = 3
        };

        Standings = [teamB, teamA];

        var matches = new MatchResultModel[]
        {
            new() {
                Id = Guid.NewGuid(),
                GroupId = GroupId,
                MatchId = Guid.NewGuid(),
                HomeTeamId = teamAId,
                HomeTeamName = "Team A",
                HomeScore = 2,
                AwayTeamId = teamBId,
                AwayTeamName = "Team B",
                AwayScore = 1,
                IsPlayed = true,
                Round = 1
            },
            new() {
                Id = Guid.NewGuid(),
                GroupId = GroupId,
                MatchId = Guid.NewGuid(),
                HomeTeamId = teamBId,
                HomeTeamName = "Team B",
                HomeScore = 2,
                AwayTeamId = teamAId,
                AwayTeamName = "Team A",
                AwayScore = 1,
                IsPlayed = true,
                Round = 2
            }
        };

        Repository.GetMatchResultsByGroupIdAsync(GroupId, CancellationToken.None)
            .ReturnsForAnyArgs(matches);

        return Task.CompletedTask;
    }

    [Test]
    public void ShouldApplyHeadToHeadTiebreaker()
    {
        teamA.Position.ShouldBe(1);
        teamB.Position.ShouldBe(2);
    }

    [Test]
    public void ShouldMarkBothAsQualified()
    {
        teamA.QualifiesForKnockout.ShouldBeTrue();
        teamB.QualifiesForKnockout.ShouldBeTrue();
    }
}

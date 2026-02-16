using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.ReadModels.Projections.UnitTests.WhenProjectingMatchResults;

public class WithValidMatchPlayed : WhenProjectingMatchResults
{
    private Guid groupId;
    private Guid matchId;
    private Guid homeTeamId;
    private Guid awayTeamId;

    protected override void Given()
    {
        base.Given();

        groupId = Guid.NewGuid();
        matchId = Guid.NewGuid();
        homeTeamId = Guid.NewGuid();
        awayTeamId = Guid.NewGuid();

        Event = new MatchPlayed(
            GroupId: groupId,
            GroupName: "Group A",
            MatchId: matchId,
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
    }

    [Test]
    public void ShouldAddMatchResultToRepository()
    {
        Repository.Received(1).Add(Arg.Any<MatchResultModel>());
    }

    [Test]
    public void ShouldMapAllEventData()
    {
        Repository.Received(1).Add(Arg.Is<MatchResultModel>(m =>
            m.GroupId == groupId &&
            m.GroupName == "Group A" &&
            m.MatchId == matchId &&
            m.Round == 1 &&
            m.IsPlayed == true &&
            m.HomeTeamId == homeTeamId &&
            m.HomeTeamName == "Home Team" &&
            m.HomeScore == 2 &&
            m.AwayTeamId == awayTeamId &&
            m.AwayTeamName == "Away Team" &&
            m.AwayScore == 1
        ));
    }

    [Test]
    public void ShouldGenerateNewId()
    {
        Repository.Received(1).Add(Arg.Is<MatchResultModel>(m => m.Id != Guid.Empty));
    }

    [Test]
    public void ShouldSetPlayedAtTimestamp()
    {
        Repository.Received(1).Add(Arg.Is<MatchResultModel>(m => m.PlayedAt != default(DateTime)));
    }
}

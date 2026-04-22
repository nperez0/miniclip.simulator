using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Application.Queries.UnitTests.Groups.V1.WhenGettingGroupStandings;

public class WithNoStandings : WhenGettingGroupStandings
{
    private Guid groupId;

    protected override Task GivenScenarioAsync()
    {
        groupId = Guid.NewGuid();
        Query = new GroupStandingsQuery(groupId);

        StandingsRepository.GetStandingsByGroupIdAsync(groupId, CancellationToken.None)
            .ReturnsForAnyArgs([]);

        MatchResultsRepository.GetMatchResultsByGroupIdAsync(groupId, CancellationToken.None)
            .ReturnsForAnyArgs([]);

        return Task.CompletedTask;
    }

    [Test]
    public void ShouldReturnEmptyDto()
    {
        Result.ShouldNotBeNull();
    }

    [Test]
    public void ShouldReturnEmptyGroupId()
    {
        Result.Value!.GroupId.ShouldBe(Guid.Empty);
    }

    [Test]
    public void ShouldReturnEmptyGroupName()
    {
        Result.Value!.GroupName.ShouldBeEmpty();
    }

    [Test]
    public void ShouldReturnDefaultStandings()
    {
        Result.Value!.Standings.Length.ShouldBe(1);
        Result.Value!.Standings[0].TeamId.ShouldBe(Guid.Empty);
    }

    [Test]
    public void ShouldReturnEmptyMatchResults()
    {
        Result.Value!.MatchResults.ShouldBeEmpty();
    }
}

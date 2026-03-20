using FluentAssertions;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;
using Miniclip.Simulator.ReadModels.Models;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Queries.UnitTests.Groups.V1.WhenGettingGroupStandings;

public class WithNoStandings : WhenGettingGroupStandings
{
    private Guid groupId;

    protected override void Given()
    {
        base.Given();

        groupId = Guid.NewGuid();
        Query = new GroupStandingsQuery(groupId);

        StandingsRepository.GetStandingsByGroupIdAsync(groupId, default)
            .ReturnsForAnyArgs([]);

        MatchResultsRepository.GetMatchResultsByGroupIdAsync(groupId, default)
            .ReturnsForAnyArgs([]);
    }

    [Test]
    public void ShouldReturnEmptyDto()
    {
        Result.Should().NotBeNull();
    }

    [Test]
    public void ShouldReturnEmptyGroupId()
    {
        Result.Value!.GroupId.Should().Be(Guid.Empty);
    }

    [Test]
    public void ShouldReturnEmptyGroupName()
    {
        Result.Value!.GroupName.Should().BeEmpty();
    }

    [Test]
    public void ShouldReturnDefaultStandings()
    {
        Result.Value!.Standings.Should().HaveCount(1);
        Result.Value!.Standings[0].TeamId.Should().Be(Guid.Empty);
    }

    [Test]
    public void ShouldReturnEmptyMatchResults()
    {
        Result.Value!.MatchResults.Should().BeEmpty();
    }
}

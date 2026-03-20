using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WithValidTeam : WhenAddingTeam
{
    protected override void Given()
    {
        Group = Group.Create(Guid.NewGuid(), "Group A", 4).Value;
        Team = Team.Create(Guid.NewGuid(), "Team 1", 80).Value!;
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.ShouldNotBeNull();
        Result!.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldAddTeamToGroup()
    {
        Group!.Teams.ShouldContain(Team!);
    }

    [Test]
    public void ShouldIncreaseTeamCount()
    {
        Group!.Teams.Count().ShouldBe(1);
    }
}

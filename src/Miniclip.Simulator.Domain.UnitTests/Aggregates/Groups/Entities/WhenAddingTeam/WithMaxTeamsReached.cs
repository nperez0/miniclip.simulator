using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WithMaxTeamsReached : WhenAddingTeam
{
    protected override void Given()
    {
        Group = Group.Create(Guid.NewGuid(), "Group A", 2).Value;
        
        // Fill the group to capacity
        Group!.AddTeam(Team.Create(Guid.NewGuid(), "Team 1", 80).Value!);
        Group!.AddTeam(Team.Create(Guid.NewGuid(), "Team 2", 70).Value!);

        // Try to add one more team
        Team = Team.Create(Guid.NewGuid(), "Team 3", 60).Value!;
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnMaxTeamsReachedException()
    {
        Result!.Exception.ShouldBeOfType<GroupAddTeamException>();
        Result!.Exception.Message.ShouldContain("maximum");
    }

    [Test]
    public void ShouldNotAddTeamToGroup()
    {
        Group!.Teams.ShouldNotContain(Team!);
    }

    [Test]
    public void ShouldMaintainTeamCount()
    {
        Group!.Teams.Count().ShouldBe(2);
    }
}

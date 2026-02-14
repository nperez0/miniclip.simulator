using FluentAssertions;
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
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnMaxTeamsReachedException()
    {
        Result!.Exception.Should().BeOfType<GroupAddTeamException>();
        Result!.Exception.Message.Should().Contain("maximum");
    }

    [Test]
    public void ShouldNotAddTeamToGroup()
    {
        Group!.Teams.Should().NotContain(Team!);
    }

    [Test]
    public void ShouldMaintainTeamCount()
    {
        Group!.Teams.Should().HaveCount(2);
    }
}

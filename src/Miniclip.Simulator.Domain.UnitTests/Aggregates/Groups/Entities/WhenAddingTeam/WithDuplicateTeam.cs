using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WithDuplicateTeam : WhenAddingTeam
{
    protected override void Given()
    {
        Group = Group.Create(Guid.NewGuid(), "Group A", 4).Value;
        Team = Team.Create(Guid.NewGuid(), "Team 1", 80).Value!;
        
        // Add the team first time
        Group!.AddTeam(Team);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnTeamAlreadyExistsException()
    {
        Result!.Exception.Should().BeOfType<GroupAddTeamException>();
        Result!.Exception.Message.Should().Contain("already exists");
    }

    [Test]
    public void ShouldNotDuplicateTeam()
    {
        Group!.Teams.Should().HaveCount(1);
    }

    [Test]
    public void ShouldKeepOriginalTeam()
    {
        Group!.Teams.Should().Contain(Team!);
    }
}

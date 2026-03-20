using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WithMaxTeamsReached : WhenAddingTeam
{
    protected override void Given()
    {
        const int capacity = 2;

        Group = GroupMother.Default(capacity);

        // Fill the group to capacity
        foreach (var team in TeamMother.Many(capacity))
            Group!.AddTeam(team);

        // Try to add one more team
        Team = TeamMother.Default();
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

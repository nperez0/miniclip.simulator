using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WithDuplicateTeam : WhenAddingTeam
{
    protected override void Given()
    {
        Group = GroupMother.Default();
        Team = TeamInfoMother.Default();

        // Add the team first time
        Group!.AddTeam(Team);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnTeamAlreadyExistsError()
    {
        Result!.Error.Code.ShouldBe("GROUP_TEAM_ALREADY_EXISTS");
        Result!.Error.Message.ShouldBe(GroupAddTeamErrors.TeamAlreadyExists(Team!.Id).Message);
    }

    [Test]
    public void ShouldNotDuplicateTeam()
    {
        Group!.Teams.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldKeepOriginalTeam()
    {
        Group!.Teams.ShouldContain(Team!);
    }
}

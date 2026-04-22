using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

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
        Result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnTeamAlreadyExistsError()
    {
        Result!.Error.Type.ShouldBe(ErrorType.Conflict);
        Result!.Error.Code.ShouldBe(GroupAddTeamErrors.TeamAlreadyExistsCode);
        Result!.Error.Messages[0].ShouldBe(GroupAddTeamErrors.TeamAlreadyExists(Team!.Id).Messages[0]);
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

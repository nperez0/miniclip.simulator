using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WithMaxTeamsReached : WhenAddingTeam
{
    private const int Capacity = 2;

    protected override void Given()
    {
        Group = GroupMother.Default(Capacity);

        // Fill the group to capacity
        foreach (var team in TeamInfoMother.Many(Capacity))
            Group!.AddTeam(team);

        // Try to add one more team
        Team = TeamInfoMother.Default();
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnMaxTeamsReachedError()
    {
        Result!.Error.Type.ShouldBe(ErrorType.Conflict);
        Result!.Error.Code.ShouldBe(GroupAddTeamErrors.MaxTeamsReachedCode);
        Result!.Error.Messages[0].ShouldBe($"Has reached the maximum number of teams: {Capacity}.");
    }

    [Test]
    public void ShouldNotAddTeamToGroup()
    {
        Group!.Teams.ShouldNotContain(Team!);
    }

    [Test]
    public void ShouldMaintainTeamCount()
    {
        Group!.Teams.Count.ShouldBe(2);
    }
}

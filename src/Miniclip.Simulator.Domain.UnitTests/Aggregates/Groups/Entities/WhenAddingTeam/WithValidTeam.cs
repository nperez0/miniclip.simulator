namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WithValidTeam : WhenAddingTeam
{
    protected override void Given()
    {
        Group = GroupMother.Default();
        Team = TeamInfoMother.Default();
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
        Group!.Teams.Count.ShouldBe(1);
    }
}

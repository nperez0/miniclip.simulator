using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WithInsufficientTeams : WhenGeneratingFixtures
{
    protected override void SetupScenario()
    {
        Capacity = 4;

        (Group, _) = GroupMother.WithTeams(2, Capacity);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotCallFixtureSchedulerFactory()
    {
        FixtureSchedulerFactory!.DidNotReceive().Create(Arg.Any<Group>());
    }

    [Test]
    public void ShouldNotCallSchedulerGenerateSchedule()
    {
        FixtureScheduler!.DidNotReceive().GenerateSchedule();
    }

    [Test]
    public void ShouldIndicateExpectedAndActualTeamCount()
    {
        Result!.Error.Type.ShouldBe(ErrorType.Conflict);
        Result!.Error.Code.ShouldBe(GroupGenerateFixturesErrors.InvalidTeamCountCode);
        Result!.Error.Messages[0].ShouldBe($"Group must have exactly {Capacity} teams to generate fixtures. Current count: {Group!.Teams.Count}.");
    }

    [Test]
    public void ShouldNotGenerateAnyMatches()
    {
        Group!.Matches.ShouldBeEmpty();
    }
}

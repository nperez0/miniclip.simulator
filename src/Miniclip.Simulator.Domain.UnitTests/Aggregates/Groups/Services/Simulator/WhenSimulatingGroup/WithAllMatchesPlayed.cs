using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithAllMatchesPlayed : WhenSimulatingGroup
{
    protected override void GivenScenario()
    {
        (Group, var teams) = GroupMother.WithTeams(2);

        Group!.AddMatch(Guid.NewGuid(), teams[0], teams[1], 1);
        Group!.Matches.First().SimulateResult(2, 1);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnAllMatchesPlayedError()
    {
        Result!.Error.Type.ShouldBe(ErrorType.Conflict);
        Result!.Error.Code.ShouldBe(GroupSimulationErrors.AllMatchesPlayedCode);
        Result!.Error.Messages[0].ShouldBe($"All matches for group '{Group!.Id}' have already been played.");
    }

    [Test]
    public void ShouldNotCallMatchSimulator()
    {
        MatchSimulator!.DidNotReceive().SimulateMatch(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public void ShouldCallMatchSimulatorFactory()
    {
        MatchSimulatorFactory!.Received(1).Create(Group!);
    }
}

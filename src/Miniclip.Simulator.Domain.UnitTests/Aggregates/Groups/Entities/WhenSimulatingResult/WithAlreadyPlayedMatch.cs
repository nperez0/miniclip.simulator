using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenSimulatingResult;

public class WithAlreadyPlayedMatch : WhenSimulatingResult
{
    protected override void Given()
    {
        GivenMatchWithTeams();

        // Simulate the match first time
        Match!.SimulateResult(3, 1);

        // Try to simulate again
        HomeScore = 2;
        AwayScore = 2;
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnAlreadyPlayedError()
    {
        Result!.Error.Type.ShouldBe(ErrorType.Conflict);
        Result!.Error.Code.ShouldBe(GroupSimulationErrors.AlreadyPlayedCode);
        Result!.Error.Messages[0].ShouldBe($"Match '{Match!.Id}' has already been played.");
    }

    [Test]
    public void ShouldKeepOriginalScores()
    {
        Match!.HomeScore.ShouldBe(3);
        Match!.AwayScore.ShouldBe(1);
    }

    [Test]
    public void ShouldRemainMarkedAsPlayed()
    {
        Match!.IsPlayed.ShouldBeTrue();
    }
}

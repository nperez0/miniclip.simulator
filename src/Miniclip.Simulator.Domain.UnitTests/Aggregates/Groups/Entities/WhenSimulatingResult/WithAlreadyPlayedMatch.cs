using Shouldly;
using NUnit.Framework;

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
        Result!.Error.Code.ShouldBe("GROUP_MATCH_ALREADY_PLAYED");
        Result!.Error.Message.ShouldBe($"Match '{Match!.Id}' has already been played.");
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

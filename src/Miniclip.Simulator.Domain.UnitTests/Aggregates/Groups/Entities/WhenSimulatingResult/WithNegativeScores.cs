using Shouldly;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenSimulatingResult;

public class WithNegativeScores : WhenSimulatingResult
{
    protected override void Given()
    {
        GivenMatchWithTeams();

        HomeScore = -1;
        AwayScore = 2;
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnNegativeScoreError()
    {
        Result!.Error.Code.ShouldBe("GROUP_NEGATIVE_SCORE");
        Result!.Error.Message.ShouldBe("Scores cannot be negative.");
    }

    [Test]
    public void ShouldNotSetScores()
    {
        Match!.HomeScore.ShouldBe(0);
        Match!.AwayScore.ShouldBe(0);
    }

    [Test]
    public void ShouldNotMarkMatchAsPlayed()
    {
        Match!.IsPlayed.ShouldBeFalse();
    }
}

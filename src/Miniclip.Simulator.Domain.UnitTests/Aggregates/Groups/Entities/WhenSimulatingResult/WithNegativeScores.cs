using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
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
    public void ShouldReturnNegativeScoreException()
    {
        Result!.Exception.ShouldBeOfType<GroupSimulationException>();
        Result!.Exception.Message.ShouldBe("Scores cannot be negative.");
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

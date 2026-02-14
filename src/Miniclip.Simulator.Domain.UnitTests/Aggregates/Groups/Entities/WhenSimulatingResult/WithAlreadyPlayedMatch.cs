using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
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
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnAlreadyPlayedException()
    {
        Result!.Exception.Should().BeOfType<MatchSimulateResultException>();
        Result!.Exception.Message.Should().Contain("already");
    }

    [Test]
    public void ShouldKeepOriginalScores()
    {
        Match!.HomeScore.Should().Be(3);
        Match!.AwayScore.Should().Be(1);
    }

    [Test]
    public void ShouldRemainMarkedAsPlayed()
    {
        Match!.IsPlayed.Should().BeTrue();
    }
}

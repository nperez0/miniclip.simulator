using FluentAssertions;
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
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnNegativeScoreException()
    {
        Result!.Exception.Should().BeOfType<MatchSimulateResultException>();
        Result!.Exception.Message.Should().Contain("negative");
    }

    [Test]
    public void ShouldNotSetScores()
    {
        Match!.HomeScore.Should().Be(0);
        Match!.AwayScore.Should().Be(0);
    }

    [Test]
    public void ShouldNotMarkMatchAsPlayed()
    {
        Match!.IsPlayed.Should().BeFalse();
    }
}

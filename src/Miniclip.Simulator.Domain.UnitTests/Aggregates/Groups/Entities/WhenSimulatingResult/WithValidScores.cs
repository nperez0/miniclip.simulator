using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenSimulatingResult;

public class WithValidScores : WhenSimulatingResult
{
    protected override void Given()
    {
        GivenMatchWithTeams();

        HomeScore = 2;
        AwayScore = 1;
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ShouldSetHomeScore()
    {
        Match!.HomeScore.Should().Be(HomeScore);
    }

    [Test]
    public void ShouldSetAwayScore()
    {
        Match!.AwayScore.Should().Be(AwayScore);
    }

    [Test]
    public void ShouldMarkMatchAsPlayed()
    {
        Match!.IsPlayed.Should().BeTrue();
    }
}

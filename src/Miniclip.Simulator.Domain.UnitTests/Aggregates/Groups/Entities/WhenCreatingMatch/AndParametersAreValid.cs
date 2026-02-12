using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingMatch;

public class AndParametersAreValid : WhenCreatingMatch
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        HomeTeam = Team.Create(Guid.NewGuid(), "Home Team", 70).Value!;
        AwayTeam = Team.Create(Guid.NewGuid(), "Away Team", 60).Value!;
        Round = 1;
    }

    [Test]
    public void ShouldCreateAMatchCorrectly()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
        Result.Value.Should().NotBeNull();
        Result.Value!.Id.Should().Be(Id);
        Result.Value.HomeTeam.Should().Be(HomeTeam);
        Result.Value.AwayTeam.Should().Be(AwayTeam);
        Result.Value.Round.Should().Be(Round);
    }

    [Test]
    public void ShouldHaveIsPlayedSetToFalse()
    {
        Result!.Value!.IsPlayed.Should().BeFalse();
    }

    [Test]
    public void ShouldHaveZeroScores()
    {
        Result!.Value!.HomeScore.Should().Be(0);
        Result!.Value!.AwayScore.Should().Be(0);
    }
}

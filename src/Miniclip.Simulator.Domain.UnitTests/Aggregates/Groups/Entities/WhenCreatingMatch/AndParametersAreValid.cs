using Shouldly;
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
        Result.ShouldNotBeNull();
        Result!.IsSuccess.ShouldBeTrue();
        Result.Value.ShouldNotBeNull();
        Result.Value!.Id.ShouldBe(Id);
        Result.Value.HomeTeam.ShouldBe(HomeTeam);
        Result.Value.AwayTeam.ShouldBe(AwayTeam);
        Result.Value.Round.ShouldBe(Round);
    }

    [Test]
    public void ShouldHaveIsPlayedSetToFalse()
    {
        Result!.Value!.IsPlayed.ShouldBeFalse();
    }

    [Test]
    public void ShouldHaveZeroScores()
    {
        Result!.Value!.HomeScore.ShouldBe(0);
        Result!.Value!.AwayScore.ShouldBe(0);
    }
}

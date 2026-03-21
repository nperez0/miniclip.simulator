using NUnit.Framework;
using Shouldly;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingMatch;

public class AndParametersAreValid : WhenCreatingMatch
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        HomeTeam = TeamInfoMother.Default();
        AwayTeam = TeamInfoMother.Default();
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

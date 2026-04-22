
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
        Result.ShouldNotBeNull();
        Result!.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldSetHomeScore()
    {
        Match!.HomeScore.ShouldBe(HomeScore);
    }

    [Test]
    public void ShouldSetAwayScore()
    {
        Match!.AwayScore.ShouldBe(AwayScore);
    }

    [Test]
    public void ShouldMarkMatchAsPlayed()
    {
        Match!.IsPlayed.ShouldBeTrue();
    }
}

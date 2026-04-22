using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WithValidGroup : WhenGeneratingFixtures
{
    private List<(TeamInfo HomeTeam, TeamInfo AwayTeam, int Round)> mockSchedule = null!;

    protected override void GivenScenario()
    {
        Capacity = 4;

        (Group, var teams) = GroupMother.WithTeams(Capacity);

        mockSchedule = [];
        int matchCount = 0;
        for (int i = 0; i < teams.Length; i++)
        {
            for (int j = i + 1; j < teams.Length; j++)
            {
                mockSchedule.Add((teams[i], teams[j], (matchCount % (Capacity - 1)) + 1));
                matchCount++;
            }
        }

        FixtureScheduler!.GenerateSchedule().Returns(mockSchedule);
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.ShouldNotBeNull();
        Result!.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldCallFixtureSchedulerFactoryWithGroup()
    {
        FixtureSchedulerFactory!.Received(1).Create(Group!);
    }

    [Test]
    public void ShouldCallGenerateScheduleOnScheduler()
    {
        FixtureScheduler!.Received(1).GenerateSchedule();
    }

    [Test]
    public void ShouldAddAllMatchesToGroup()
    {
        var expectedMatchCount = mockSchedule.Count;
        Group!.Matches.Count.ShouldBe(expectedMatchCount);
    }

    [Test]
    public void ShouldCreateMatchesWithCorrectTeams()
    {
        foreach (var (homeTeam, awayTeam, round) in mockSchedule)
        {
            Group!.Matches.ShouldContain(m =>
                m.HomeTeam == homeTeam &&
                m.AwayTeam == awayTeam &&
                m.Round == round);
        }
    }

    [Test]
    public void ShouldNotHaveTeamPlayingItself()
    {
        Group!.Matches.ShouldAllBe(m => m.HomeTeam != m.AwayTeam);
    }

    [Test]
    public void ShouldHaveAllMatchesUnplayed()
    {
        Group!.Matches.ShouldAllBe(m => m.IsPlayed == false);
    }
}

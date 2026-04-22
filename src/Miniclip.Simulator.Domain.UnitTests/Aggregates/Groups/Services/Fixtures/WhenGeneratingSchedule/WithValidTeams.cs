using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingSchedule;

[TestFixture(2)]
[TestFixture(3)]
[TestFixture(4)]
[TestFixture(5)]
[TestFixture(6)]
public class WithValidTeams(int capacity) : WhenGeneratingSchedule
{
    protected override void Given()
    {
        Capacity = capacity;
        Teams = TeamInfoMother.Many(Capacity);
    }

    [Test]
    public void ShouldGenerateCorrectNumberOfMatches()
    {
        // Each team plays every other team once: n * (n-1) / 2
        var expectedMatches = Capacity * (Capacity - 1) / 2;

        Schedule!.Count().ShouldBe(expectedMatches);
    }

    [Test]
    public void ShouldHaveCorrectNumberOfRounds()
    {
        // For odd capacity, rounds = capacity (each team gets one bye)
        // For even capacity, rounds = capacity - 1
        var isOdd = Capacity % 2 == 1;
        var expectedRounds = isOdd ? Capacity : Capacity - 1;
        var actualRounds = Schedule!
            .Select(m => m.Round)
            .Distinct()
            .Count();

        actualRounds.ShouldBe(expectedRounds);
    }

    [Test]
    public void ShouldNotContainDummyTeams()
    {
        Schedule!.ShouldNotContain(m => m.HomeTeam == TeamInfo.Dummy || m.AwayTeam == TeamInfo.Dummy);
    }

    [Test]
    public void ShouldNotHaveTeamPlayingItself()
    {
        Schedule!.ShouldAllBe(m => m.HomeTeam != m.AwayTeam);
    }

    [Test]
    public void ShouldHaveEachTeamPlayEveryOtherTeamOnce()
    {
        var teamPairings = new HashSet<string>();

        foreach (var (HomeTeam, AwayTeam, _) in Schedule!)
        {
            // Create a sorted pairing key to avoid duplicates (1-2 is same as 2-1)
            var pair = HomeTeam.Id < AwayTeam.Id
                ? $"{HomeTeam.Id}-{AwayTeam.Id}"
                : $"{AwayTeam.Id}-{HomeTeam.Id}";

            teamPairings.Add(pair);
        }

        // Each unique pairing should appear exactly once
        var expectedPairings = Capacity * (Capacity - 1) / 2;
        teamPairings.Count.ShouldBe(expectedPairings);
    }

    [Test]
    public void ShouldDistributeMatchesEvenlyAcrossRounds()
    {
        var matchesPerRound = Schedule!.GroupBy(m => m.Round)
            .Select(g => g.Count())
            .ToList();

        var expectedMatchesPerRound = Capacity / 2;

        // All rounds should have the same number of matches
        matchesPerRound.ShouldAllBe(count => count == expectedMatchesPerRound);
    }
}

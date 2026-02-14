using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WithValidGroup() : WhenGeneratingFixtures
{
    private List<(Team HomeTeam, Team AwayTeam, int Round)> mockSchedule = null!;

    protected override void Given()
    {
        base.Given();

        Capacity = 4;

        var teams = GivenGroupWithTeams(Capacity);

        // Create mock schedule data
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

        // Setup mock scheduler to return our test data
        FixtureScheduler!.GenerateSchedule().Returns(mockSchedule);
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
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
        Group!.Matches.Should().HaveCount(expectedMatchCount);
    }

    [Test]
    public void ShouldCreateMatchesWithCorrectTeams()
    {
        foreach (var (homeTeam, awayTeam, round) in mockSchedule)
        {
            Group!.Matches.Should().Contain(m => 
                m.HomeTeamId == homeTeam.Id && 
                m.AwayTeamId == awayTeam.Id &&
                m.Round == round);
        }
    }

    [Test]
    public void ShouldNotHaveTeamPlayingItself()
    {
        Group!.Matches.Should().OnlyContain(m => m.HomeTeamId != m.AwayTeamId);
    }

    [Test]
    public void ShouldHaveAllMatchesUnplayed()
    {
        Group!.Matches.Should().OnlyContain(m => m.IsPlayed == false);
    }
}

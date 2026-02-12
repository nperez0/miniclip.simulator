using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithPartiallyPlayedMatches : WhenSimulatingGroup
{
    private int alreadyPlayedCount;

    protected override void Given()
    {
        Teams = [
            Team.Create(Guid.NewGuid(), "Strong Team", 90).Value!,
            Team.Create(Guid.NewGuid(), "Medium Team", 60).Value!,
            Team.Create(Guid.NewGuid(), "Weak Team", 30).Value!,
            Team.Create(Guid.NewGuid(), "Average Team", 50).Value!
        ];

        GivenGroupWithTeamsAndGeneratedFixtures();

        // Simulate some matches manually
        var firstMatch = Group!.Matches.First();
        var secondMatch = Group.Matches.Skip(1).First();
        
        firstMatch.SimulateResult(3, 1);
        secondMatch.SimulateResult(2, 2);

        alreadyPlayedCount = 2;

        // Mock the match simulator for remaining matches
        MatchSimulator!.SimulateMatch(Arg.Any<int>(), Arg.Any<int>())
            .Returns((1, 0));
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ShouldSimulateAllMatches()
    {
        Group!.Matches.Should().OnlyContain(m => m.IsPlayed);
    }

    [Test]
    public void ShouldNotChangeAlreadyPlayedMatches()
    {
        var playedMatches = Group!.Matches.Take(alreadyPlayedCount).ToList();

        playedMatches[0].HomeScore.Should().Be(3);
        playedMatches[0].AwayScore.Should().Be(1);
        playedMatches[1].HomeScore.Should().Be(2);
        playedMatches[1].AwayScore.Should().Be(2);
    }

    [Test]
    public void ShouldOnlyCallMatchSimulatorForUnplayedMatches()
    {
        var expectedCalls = Group!.Matches.Count - alreadyPlayedCount;
        MatchSimulator!.Received(expectedCalls).SimulateMatch(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public void ShouldSetScoresForNewlySimulatedMatches()
    {
        var newlyPlayed = Group!.Matches.Skip(alreadyPlayedCount).ToList();
        newlyPlayed.Should().OnlyContain(m => m.HomeScore == 1 && m.AwayScore == 0);
    }
}

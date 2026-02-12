using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithValidGroup : WhenSimulatingGroup
{
    protected override void Given()
    {
        Teams = [
            Team.Create(Guid.NewGuid(), "Strong Team", 90).Value!,
            Team.Create(Guid.NewGuid(), "Medium Team", 60).Value!,
            Team.Create(Guid.NewGuid(), "Weak Team", 30).Value!,
            Team.Create(Guid.NewGuid(), "Average Team", 50).Value!
        ];

        GivenGroupWithTeamsAndGeneratedFixtures();

        // Mock the match simulator to return predictable scores
        MatchSimulator!.SimulateMatch(Arg.Any<int>(), Arg.Any<int>())
            .Returns((2, 1));
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
    public void ShouldSetScoresForAllMatches()
    {
        Group!.Matches.Should().OnlyContain(m => m.HomeScore == 2 && m.AwayScore == 1);
    }

    [Test]
    public void ShouldCallMatchSimulatorForEachMatch()
    {
        var expectedMatchCount = Group!.Matches.Count;
        MatchSimulator!.Received(expectedMatchCount).SimulateMatch(Arg.Any<int>(), Arg.Any<int>());
    }
}

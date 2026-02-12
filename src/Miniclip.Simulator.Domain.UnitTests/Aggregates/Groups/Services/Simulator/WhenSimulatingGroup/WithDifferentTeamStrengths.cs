using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithDifferentTeamStrengths : WhenSimulatingGroup
{
    protected override void Given()
    {
        Teams = [
            Team.Create(Guid.NewGuid(), "Strong Team", 90).Value!,
            Team.Create(Guid.NewGuid(), "Medium Team", 60).Value!,
            Team.Create(Guid.NewGuid(), "Weak Team", 30).Value!,
            Team.Create(Guid.NewGuid(), "Average Team", 50).Value!
        ];

        AssumeExistingGroupWithTeamsAndGeneratedFixtures();
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ShouldCallMatchSimulatorWithCorrectStrengths()
    {
        // Verify the simulator was called with actual team strengths
        MatchSimulator!.Received().SimulateMatch(90, Arg.Any<int>());
        MatchSimulator!.Received().SimulateMatch(60, Arg.Any<int>());
        MatchSimulator!.Received().SimulateMatch(30, Arg.Any<int>());
        MatchSimulator!.Received().SimulateMatch(50, Arg.Any<int>());
    }

    [Test]
    public void ShouldSimulateAllMatches()
    {
        Group!.Matches.Should().OnlyContain(m => m.IsPlayed);
    }
}

using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithNegativeScoresFromSimulator : WhenSimulatingGroup
{
    protected override void Given()
    {
        Teams = [
            Team.Create(Guid.NewGuid(), "Team A", 70).Value!,
            Team.Create(Guid.NewGuid(), "Team B", 60).Value!
        ];

        GivenGroupWithTeamsAndGeneratedFixtures();

        // Mock the match simulator to return invalid negative scores
        MatchSimulator!.SimulateMatch(Arg.Any<int>(), Arg.Any<int>())
            .Returns((-1, 2));
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldHaveException()
    {
        Result!.Exception.Should().NotBeNull();
        Result!.Exception.Message.Should().Contain("negative");
    }

    [Test]
    public void ShouldStopSimulatingAfterFirstError()
    {
        // Should only be called once before stopping
        MatchSimulator!.Received(1).SimulateMatch(Arg.Any<int>(), Arg.Any<int>());
    }
}

using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithAllMatchesPlayed : WhenSimulatingGroup
{
    protected override void Given()
    {
        base.Given();

        var teams = GivenGroupWithTeams(2);

        // Add and simulate match
        Group!.AddMatch(Guid.NewGuid(), teams[0], teams[1], 1);
        Group!.Matches.First().SimulateResult(2, 1);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnAllMatchesPlayedException()
    {
        Result!.Exception.Should().BeOfType<GroupSimulationException>();
    }

    [Test]
    public void ShouldNotCallMatchSimulator()
    {
        MatchSimulator!.DidNotReceive().SimulateMatch(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public void ShouldCallMatchSimulatorFactory()
    {
        MatchSimulatorFactory!.Received(1).Create(Group!);
    }
}

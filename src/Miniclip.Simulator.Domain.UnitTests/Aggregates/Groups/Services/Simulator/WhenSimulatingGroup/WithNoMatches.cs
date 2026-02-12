using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithNoMatches : WhenSimulatingGroup
{
    protected override void Given()
    {
        Teams = [
            Team.Create(Guid.NewGuid(), "Team A", 70).Value!,
            Team.Create(Guid.NewGuid(), "Team B", 60).Value!
        ];

        AssumeExistingGroupWithTeams();

        // No fixtures generated - group has no matches
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ShouldNotCallMatchSimulator()
    {
        MatchSimulator!.DidNotReceive().SimulateMatch(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public void ShouldHaveNoMatches()
    {
        Group!.Matches.Should().BeEmpty();
    }
}

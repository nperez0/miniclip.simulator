using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithAllMatchesAlreadyPlayed : WhenSimulatingGroup
{
    protected override void Given()
    {
        Teams = [
            Team.Create(Guid.NewGuid(), "Team A", 70).Value!,
            Team.Create(Guid.NewGuid(), "Team B", 60).Value!
        ];

        GivenGroupWithTeamsAndGeneratedFixtures();

        // Simulate all matches manually
        foreach (var match in Group!.Matches)
            match.SimulateResult(2, 1);
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
    public void ShouldKeepExistingScores()
    {
        Group!.Matches.Should().OnlyContain(m => m.HomeScore == 2 && m.AwayScore == 1);
    }
}

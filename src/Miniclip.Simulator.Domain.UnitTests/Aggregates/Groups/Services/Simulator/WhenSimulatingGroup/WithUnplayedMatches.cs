using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WithUnplayedMatches : WhenSimulatingGroup
{
    protected override void Given()
    {
        base.Given();

        var teams = GivenGroupWithTeams(3);

        // Add matches
        Group!.AddMatch(Guid.NewGuid(), teams[0], teams[1], 1);
        Group!.AddMatch(Guid.NewGuid(), teams[0], teams[2], 2);
        Group!.AddMatch(Guid.NewGuid(), teams[1], teams[2], 3);

        // Setup mocks
        MatchSimulator!.SimulateMatch(Arg.Any<int>(), Arg.Any<int>()).Returns((2, 1));
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ShouldCallMatchSimulatorFactoryWithGroup()
    {
        MatchSimulatorFactory!.Received(1).Create(Group!);
    }

    [Test]
    public void ShouldCallSimulateMatchForEachUnplayedMatch()
    {
        var matchCount = Group!.Matches.Count;
        MatchSimulator!.Received(matchCount).SimulateMatch(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public void ShouldMarkAllMatchesAsPlayed()
    {
        Group!.Matches.Should().OnlyContain(m => m.IsPlayed);
    }

    [Test]
    public void ShouldSetScoresForAllMatches()
    {
        Group!.Matches.Should().OnlyContain(m => m.HomeScore == 2 && m.AwayScore == 1);
    }
}

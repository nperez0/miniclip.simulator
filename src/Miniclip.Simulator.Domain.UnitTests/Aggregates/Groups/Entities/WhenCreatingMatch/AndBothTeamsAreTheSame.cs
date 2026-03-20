using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingMatch;

public class AndBothTeamsAreTheSame : WhenCreatingMatch
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        HomeTeam = Team.Create(Guid.NewGuid(), "Team A", 70).Value!;
        AwayTeam = HomeTeam; // Same team
        Round = 1;
    }

    [Test]
    public void ShouldReturnAnException()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Exception.ShouldBeOfType<GroupGenerateFixturesException>();
        Result.Exception.Message.ShouldContain("cannot play against itself");
    }
}

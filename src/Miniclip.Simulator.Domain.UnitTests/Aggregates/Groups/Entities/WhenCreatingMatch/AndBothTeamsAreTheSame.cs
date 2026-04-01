using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingMatch;

public class AndBothTeamsAreTheSame : WhenCreatingMatch
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        HomeTeam = TeamInfoMother.Default();
        AwayTeam = HomeTeam; // Same team
        Round = 1;
    }

    [Test]
    public void ShouldReturnAnError()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Code.ShouldBe("GROUP_SAME_TEAM");
        Result.Error.Message.ShouldBe(GroupGenerateFixturesErrors.SameTeam().Message);
    }
}

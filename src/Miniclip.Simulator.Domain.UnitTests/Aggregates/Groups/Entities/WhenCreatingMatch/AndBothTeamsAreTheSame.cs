using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;
using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

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
        Result.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Type.ShouldBe(ErrorType.Conflict);
        Result.Error.Code.ShouldBe(GroupGenerateFixturesErrors.SameTeamCode);
        Result.Error.Messages[0].ShouldBe($"A team cannot play against itself. Team ID: {HomeTeam!.Id}");
    }
}

using Shouldly;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Teams.Entities.WhenCreatingTeam;

[TestFixture(-1)]
[TestFixture(101)]
[TestFixture(-100)]
[TestFixture(150)]
public class AndStrengthIsInvalid(int strength) : WhenCreatingTeam
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = "Team A";
        Strength = strength;
    }

    [Test]
    public void ShouldReturnAnError()
    {
        Result.ShouldNotBeNull();
        Result.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Code.ShouldBe("TEAM_STRENGTH_INVALID");
        Result.Error.Message.ShouldBe($"Strength '{strength}' must be between 0 and 100.");
    }
}

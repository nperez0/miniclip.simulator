using Miniclip.Core;
using NUnit.Framework;
using Shouldly;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Teams.Entities.WhenCreatingTeam;

[TestFixture("")]
[TestFixture(null)]
[TestFixture(" ")]
public class AndNameIsInvalid(string name) : WhenCreatingTeam
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = name;
        Strength = 50;
    }

    [Test]
    public void ShouldReturnAnError()
    {
        Result.ShouldNotBeNull();
        Result.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Type.ShouldBe(ErrorType.Validation);
        Result.Error.Code.ShouldBe("TEAM_VALIDATION_FAILED");
        Result.Error.Messages.ShouldBe([$"Team name '{Name}' cannot be empty."]);
    }
}

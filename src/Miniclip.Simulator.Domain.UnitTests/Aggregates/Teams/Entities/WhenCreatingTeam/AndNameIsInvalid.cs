using Shouldly;
using NUnit.Framework;

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
        Result.Error.Code.ShouldBe("TEAM_NAME_EMPTY");
        Result.Error.Message.ShouldBe($"Team name '{name}' cannot be empty.");
    }
}

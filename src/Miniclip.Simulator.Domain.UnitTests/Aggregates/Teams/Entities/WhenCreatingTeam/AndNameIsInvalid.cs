using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Teams.Exceptions;
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
    public void ShouldReturnAnException()
    {
        Result.ShouldNotBeNull();
        Result.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Exception.ShouldBeOfType<TeamCreationException>();
        Result.Exception.Message.ShouldBe($"Team name '{name}' cannot be empty.");
    }
}

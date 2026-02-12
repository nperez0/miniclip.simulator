using FluentAssertions;
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
        Result.Should().NotBeNull();
        Result.IsFailure.Should().BeTrue();
        Result.Value.Should().BeNull();
        Result.Exception.Should().BeOfType<TeamCreationException>();
        Result.Exception.Message.Should().Be($"Team name '{name}' cannot be empty.");
    }
}

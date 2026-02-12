using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Exceptions;
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
    public void ShouldReturnAnException()
    {
        Result.Should().NotBeNull();
        Result.IsFailure.Should().BeTrue();
        Result.Value.Should().BeNull();
        Result.Exception.Should().BeOfType<TeamCreationException>();
        Result.Exception.Message.Should().Be($"Strength '{strength}' must be between 0 and 100.");
    }
}

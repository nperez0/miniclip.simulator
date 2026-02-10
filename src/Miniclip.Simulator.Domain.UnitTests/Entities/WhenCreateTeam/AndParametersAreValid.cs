using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Entities.WhenCreateTeam;

public class AndParametersAreValid : WhenCreateTeam
{
    protected override void Given()
    {
        Id = 1;
        Name = "Team A";
        Strength = 50;
    }

    [Test]
    public void ShouldCreateAnActivityCorrectly()
    {
        Result.IsSuccess.Should().BeTrue();
        Result.Value.Should().NotBeNull();
        Result.Value.Id.Should().Be(Id);
        Result.Value.Name.Should().Be(Name);
        Result.Value.Strength.Should().Be(Strength);
    }
}

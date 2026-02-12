using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Teams.Entities.WhenCreatingTeam;

public class AndParametersAreValid : WhenCreatingTeam
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = "Team A";
        Strength = 50;
    }

    [Test]
    public void ShouldCreateATeamCorrectly()
    {
        Result.Should().NotBeNull();
        Result.IsSuccess.Should().BeTrue();
        Result.Value.Should().NotBeNull();
        Result.Value.Id.Should().Be(Id);
        Result.Value.Name.Should().Be(Name);
        Result.Value.Strength.Should().Be(Strength);
    }
}

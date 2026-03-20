using Shouldly;
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
        Result.ShouldNotBeNull();
        Result.IsSuccess.ShouldBeTrue();
        Result.Value.ShouldNotBeNull();
        Result.Value.Id.ShouldBe(Id);
        Result.Value.Name.ShouldBe(Name);
        Result.Value.Strength.ShouldBe(Strength);
    }
}

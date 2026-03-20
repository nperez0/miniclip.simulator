using Shouldly;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingGroup;

public class AndParametersAreValid : WhenCreatingGroup
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = "Group A";
        Capacity = 4;
    }

    [Test]
    public void ShouldCreateAGroupCorrectly()
    {
        Result.ShouldNotBeNull();
        Result!.IsSuccess.ShouldBeTrue();
        Result.Value.ShouldNotBeNull();
        Result.Value!.Id.ShouldBe(Id);
        Result.Value.Name.ShouldBe(Name);
        Result.Value.Capacity.ShouldBe(Capacity);
    }

    [Test]
    public void ShouldHaveEmptyTeamsCollection()
    {
        Result!.Value!.Teams.ShouldBeEmpty();
    }

    [Test]
    public void ShouldHaveEmptyMatchesCollection()
    {
        Result!.Value!.Matches.ShouldBeEmpty();
    }
}

using FluentAssertions;
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
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
        Result.Value.Should().NotBeNull();
        Result.Value!.Id.Should().Be(Id);
        Result.Value.Name.Should().Be(Name);
        Result.Value.Capacity.Should().Be(Capacity);
    }

    [Test]
    public void ShouldHaveEmptyTeamsCollection()
    {
        Result!.Value!.Teams.Should().BeEmpty();
    }

    [Test]
    public void ShouldHaveEmptyMatchesCollection()
    {
        Result!.Value!.Matches.Should().BeEmpty();
    }
}

using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingGroup;

public class WithMinimumCapacity : WhenCreatingGroup
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = "Minimum Group";
        Capacity = 2;
    }

    [Test]
    public void ShouldCreateGroupSuccessfully()
    {
        Result.Should().NotBeNull();
        Result!.IsSuccess.Should().BeTrue();
        Result.Value.Should().NotBeNull();
    }

    [Test]
    public void ShouldHaveCapacityOfTwo()
    {
        Result!.Value!.Capacity.Should().Be(2);
    }

    [Test]
    public void ShouldHaveCorrectName()
    {
        Result!.Value!.Name.Should().Be("Minimum Group");
    }
}

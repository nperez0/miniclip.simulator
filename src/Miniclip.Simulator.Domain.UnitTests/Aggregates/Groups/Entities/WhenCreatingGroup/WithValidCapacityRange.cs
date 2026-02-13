using FluentAssertions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingGroup;

[TestFixture(2)]
[TestFixture(3)]
[TestFixture(4)]
[TestFixture(5)]
[TestFixture(6)]
public class WithValidCapacityRange(int capacity) : WhenCreatingGroup
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = "Group A";
        Capacity = capacity;
    }

    [Test]
    public void ShouldSucceed()
    {
        Result!.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnGroup()
    {
        Result!.Value.Should().NotBeNull();
    }

    [Test]
    public void ShouldHaveCorrectCapacity()
    {
        Result!.Value!.Capacity.Should().Be(capacity);
    }
}

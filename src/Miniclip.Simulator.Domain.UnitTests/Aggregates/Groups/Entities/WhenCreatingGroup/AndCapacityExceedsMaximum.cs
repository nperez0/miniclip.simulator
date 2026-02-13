using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingGroup;

[TestFixture(7)]
[TestFixture(8)]
[TestFixture(10)]
[TestFixture(100)]
public class AndCapacityExceedsMaximum(int capacity) : WhenCreatingGroup
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = "Group A";
        Capacity = capacity;
    }

    [Test]
    public void ShouldFail()
    {
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnInvalidCapacityException()
    {
        Result!.Exception.Should().BeOfType<GroupCreationException>();
        Result.Exception!.Message.Should().Contain("between 2 and 6");
    }
}

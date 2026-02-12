using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingGroup;

[TestFixture(0)]
[TestFixture(1)]
[TestFixture(-1)]
[TestFixture(-10)]
public class AndCapacityIsInvalid(int capacity) : WhenCreatingGroup
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = "Group A";
        Capacity = capacity;
    }

    [Test]
    public void ShouldReturnAnException()
    {
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
        Result.Value.Should().BeNull();
        Result.Exception.Should().BeOfType<GroupCreationException>();
        Result.Exception.Message.Should().Contain("at least 2");
    }
}

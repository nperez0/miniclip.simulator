using Shouldly;
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
    public void ShouldReturnAnError()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Code.ShouldBe("GROUP_CAPACITY_INVALID");
        Result.Error.Message.ShouldBe(GroupCreationErrors.InvalidCapacity(Capacity, 2, 6).Message);
    }
}

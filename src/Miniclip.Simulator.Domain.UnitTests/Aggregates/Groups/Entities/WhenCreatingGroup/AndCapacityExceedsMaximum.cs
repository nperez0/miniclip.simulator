using Shouldly;
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
        Result!.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnInvalidCapacityError()
    {
        Result!.Error.Code.ShouldBe("GROUP_CAPACITY_INVALID");
        Result.Error.Message.ShouldBe(GroupCreationErrors.InvalidCapacity(Capacity, 2, 6).Message);
    }
}

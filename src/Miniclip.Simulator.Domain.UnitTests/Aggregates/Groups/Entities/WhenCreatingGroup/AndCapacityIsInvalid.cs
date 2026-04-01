using Miniclip.Core;
using NUnit.Framework;
using Shouldly;

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
        Result.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Type.ShouldBe(ErrorType.Validation);
        Result.Error.Code.ShouldBe("GROUP_VALIDATION_FAILED");
        Result.Error.Messages.ShouldBe([$"Group capacity must be between 2 and 6, but was {Capacity}."]);
    }
}

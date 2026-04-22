using Miniclip.Core;

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
        Result!.Error.Type.ShouldBe(ErrorType.Validation);
        Result!.Error.Code.ShouldBe("GROUP_VALIDATION_FAILED");
        Result!.Error.Messages.ShouldBe([$"Group capacity must be between 2 and 6, but was {Capacity}."]);
    }
}

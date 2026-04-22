using Miniclip.Core;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingGroup;

[TestFixture("")]
[TestFixture(null)]
[TestFixture(" ")]
[TestFixture("   ")]
public class AndNameIsInvalid(string? name) : WhenCreatingGroup
{
    protected override void Given()
    {
        Id = Guid.NewGuid();
        Name = name;
        Capacity = 4;
    }

    [Test]
    public void ShouldReturnAnError()
    {
        Result.ShouldNotBeNull();
        Result.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Type.ShouldBe(ErrorType.Validation);
        Result.Error.Code.ShouldBe("GROUP_VALIDATION_FAILED");
        Result.Error.Messages.ShouldBe([$"Group name '{Name}' cannot be empty."]);
    }
}

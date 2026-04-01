using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NUnit.Framework;

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
        Result!.IsFailure.ShouldBeTrue();
        Result.Value.ShouldBeNull();
        Result.Error.Code.ShouldBe("GROUP_NAME_EMPTY");
        Result.Error.Message.ShouldBe(GroupCreationErrors.EmptyName(Name).Message);
    }
}

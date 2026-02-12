using FluentAssertions;
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
    public void ShouldReturnAnException()
    {
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
        Result.Value.Should().BeNull();
        Result.Exception.Should().BeOfType<GroupCreationException>();
        Result.Exception.Message.Should().Contain("cannot be empty");
    }
}

using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingGroup;

public class WhenCreatingGroup : TestBase<Group>
{
    protected Guid Id { get; set; }
    protected string? Name { get; set; }
    protected int Capacity { get; set; }
    protected Result<Group>? Result { get; set; }

    protected override Group CreateSystemUnderTest()
        => null!;

    protected override void When()
    {
        Result = Group.Create(Id, Name, Capacity);
    }
}

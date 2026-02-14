using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenAddingTeam;

public class WhenAddingTeam : TestBase<Group>
{
    protected Group? Group { get; set; }

    protected Team? Team { get; set; }

    protected Result? Result { get; set; }

    protected override Group CreateSystemUnderTest()
        => null!;

    protected override void When()
    {
        Result = Group!.AddTeam(Team!);
    }
}

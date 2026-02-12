using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Teams.Entities.WhenCreatingTeam;

public class WhenCreatingTeam : TestBase<Team>
{
    protected Guid Id { get; set; }
    protected string? Name { get; set; }
    protected int Strength { get; set; }
    protected Result<Team>? Result { get; set; }

    protected override Team CreateSystemUnderTest()
        => null!;

    protected override void When()
    {
        Result = Team.Create(Id, Name, Strength);
    }
}

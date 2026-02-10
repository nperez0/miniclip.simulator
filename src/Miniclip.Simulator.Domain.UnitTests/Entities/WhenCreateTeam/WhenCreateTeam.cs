using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Entities.WhenCreateTeam;

public class WhenCreateTeam : TestBase<Team>
{
    protected int Id { get; set; }
    protected string? Name { get; set; }
    protected int Strength { get; set; }
    protected Result<Team> Result { get; set; }

    protected override Team CreateSystemUnderTest()
    {
        return null!;
    }

    protected override void When()
    {
        Result = Team.Create(Id, Name, Strength);
    }
}

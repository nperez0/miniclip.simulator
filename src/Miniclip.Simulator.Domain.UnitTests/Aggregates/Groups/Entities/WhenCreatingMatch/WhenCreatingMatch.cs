using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Entities.WhenCreatingMatch;

public class WhenCreatingMatch : TestBase<Match>
{
    protected Guid Id { get; set; }
    protected Team? HomeTeam { get; set; }
    protected Team? AwayTeam { get; set; }
    protected int Round { get; set; }
    protected Result<Match>? Result { get; set; }

    protected override Match CreateSystemUnderTest()
        => null!;

    protected override void When()
    {
        Result = Match.Create(Id, HomeTeam!, AwayTeam!, Round);
    }
}

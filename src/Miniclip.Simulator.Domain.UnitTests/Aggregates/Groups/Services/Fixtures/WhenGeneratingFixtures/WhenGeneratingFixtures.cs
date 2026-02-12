using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WhenGeneratingFixtures : TestBase<FixtureSchedulerService>
{
    protected int Capacity { get; set; }

    protected Group? Group { get; set; }

    protected Result? Result { get; set; }

    protected override void When()
    {
        Result = Sut!.GenerateFixtures(Group!);
    }
}

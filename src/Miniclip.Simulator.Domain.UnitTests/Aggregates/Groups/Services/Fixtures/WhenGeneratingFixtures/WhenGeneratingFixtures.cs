using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WhenGeneratingFixtures : TestBase<FixtureSchedulerService>
{
    protected IFixtureSchedulerFactory? FixtureSchedulerFactory { get; set; }

    protected IFixtureScheduler? FixtureScheduler { get; set; }

    protected int Capacity { get; set; }

    protected Group? Group { get; set; }

    protected Result? Result { get; set; }

    protected override void Given()
    {
        FixtureSchedulerFactory = Substitute.For<IFixtureSchedulerFactory>();
        FixtureScheduler = Substitute.For<IFixtureScheduler>();

        FixtureSchedulerFactory!.Create(Arg.Any<Group>()).Returns(FixtureScheduler);

        GivenScenario();
    }

    protected override FixtureSchedulerService CreateSystemUnderTest()
        => new(FixtureSchedulerFactory!);

    protected override void When()
    {
        Result = Sut!.GenerateFixtures(Group!);
    }
}

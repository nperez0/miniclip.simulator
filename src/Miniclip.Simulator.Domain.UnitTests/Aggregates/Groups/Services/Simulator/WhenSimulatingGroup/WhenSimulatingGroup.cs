using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WhenSimulatingGroup : TestBase<GroupSimulator>
{
    protected IMatchSimulatorFactory? MatchSimulatorFactory { get; set; }

    protected IMatchSimulator? MatchSimulator { get; set; }

    protected Group? Group { get; set; }

    protected Result? Result { get; private set; }

    protected override void Given()
    {
        MatchSimulatorFactory = Substitute.For<IMatchSimulatorFactory>();
        MatchSimulator = Substitute.For<IMatchSimulator>();

        MatchSimulatorFactory!.Create(Arg.Any<Group>()).Returns(MatchSimulator);

        GivenScenario();
    }

    protected override GroupSimulator CreateSystemUnderTest()
        => new(MatchSimulatorFactory!);

    protected override void When()
    {
        Result = Sut!.SimulateAllMatches(Group!);
    }
}

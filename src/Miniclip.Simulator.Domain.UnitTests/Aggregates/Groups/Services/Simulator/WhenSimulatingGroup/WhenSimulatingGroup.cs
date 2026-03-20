using AutoFixture;
using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;
using NSubstitute;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WhenSimulatingGroup : TestBase<GroupSimulator>
{
    protected IMatchSimulatorFactory? MatchSimulatorFactory { get; set; }

    protected IMatchSimulator? MatchSimulator { get; set; }

    protected Group? Group { get; set; }

    protected Result? Result { get; private set; }

    protected override void Given()
    {
        MatchSimulatorFactory = Fixture.Freeze<IMatchSimulatorFactory>();
        MatchSimulator = Fixture.Freeze<IMatchSimulator>();

        MatchSimulatorFactory!.Create(Arg.Any<Group>()).Returns(MatchSimulator);
    }

    protected override void When()
    {
        Result = Sut!.SimulateAllMatches(Group!);
    }

}

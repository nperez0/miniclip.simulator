using AutoFixture;
using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenSimulatingGroups;

public abstract class WhenSimulatingGroups : TestBase<SimulateGroupCommandHandler>
{
    protected IRepository<Group> GroupRepository { get; private set; } = null!;
    protected IGroupSimulator GroupSimulator { get; private set; } = null!;
    protected SimulateGroupCommand Command { get; set; } = null!;
    protected Result Result { get; set; } = null!;

    protected override void Given()
    {
        GroupRepository = Fixture.Freeze<IRepository<Group>>();
        GroupSimulator = Fixture.Freeze<IGroupSimulator>();
    }

    protected override void When()
    {
        Result = Sut!.Handle(Command, CancellationToken.None).Result;
    }
}

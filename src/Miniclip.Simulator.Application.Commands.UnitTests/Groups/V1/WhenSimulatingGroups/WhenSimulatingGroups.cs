using AutoFixture;
using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenSimulatingGroups;

public abstract class WhenSimulatingGroups : AsyncTestBase<SimulateGroupCommandHandler>
{
    protected IAggregateRepository<Group> GroupRepository { get; private set; } = null!;
    protected IGroupSimulator GroupSimulator { get; private set; } = null!;
    protected SimulateGroupCommand Command { get; set; } = null!;
    protected Result Result { get; set; } = null!;

    protected override Task GivenAsync()
    {
        GroupRepository = Fixture.Freeze<IAggregateRepository<Group>>();
        GroupSimulator = Fixture.Freeze<IGroupSimulator>();

        return Task.CompletedTask;
    }

    protected override async Task WhenAsync()
    {
        Result = await Sut!.Handle(Command, CancellationToken.None).ConfigureAwait(false);
    }
}

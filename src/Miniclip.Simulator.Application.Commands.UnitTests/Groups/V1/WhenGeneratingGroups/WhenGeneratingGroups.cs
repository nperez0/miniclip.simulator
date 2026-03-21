using AutoFixture;
using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenGeneratingGroups;

public abstract class WhenGeneratingGroups : AsyncTestBase<GenerateGroupCommandHandler>
{
    protected IAggregateRepository<Group> GroupRepository { get; private set; } = null!;
    protected IAggregateRepository<Team> TeamRepository { get; private set; } = null!;
    protected IFixtureSchedulerService FixtureSchedulerService { get; private set; } = null!;
    protected GenerateGroupCommand Command { get; set; } = null!;
    protected Result<Guid> Result { get; set; } = null!;

    protected override void Given()
    {
        GroupRepository = Fixture.Freeze<IAggregateRepository<Group>>();
        TeamRepository = Fixture.Freeze<IAggregateRepository<Team>>();
        FixtureSchedulerService = Fixture.Freeze<IFixtureSchedulerService>();
    }

    protected override async ValueTask WhenAsync()
    {
        Result = await Sut!.Handle(Command, CancellationToken.None).ConfigureAwait(false);
    }
}

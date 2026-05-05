using Miniclip.Core;
using Miniclip.Core.Domain;
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

    protected override Task GivenAsync()
    {
        GroupRepository = Substitute.For<IAggregateRepository<Group>>();
        TeamRepository = Substitute.For<IAggregateRepository<Team>>();
        FixtureSchedulerService = Substitute.For<IFixtureSchedulerService>();

        return SetupScenarioAsync();
    }

    protected override GenerateGroupCommandHandler CreateSystemUnderTest()
        => new(GroupRepository, TeamRepository, FixtureSchedulerService);

    protected override async Task WhenAsync()
    {
        Result = await Sut!.Handle(Command, CancellationToken.None).ConfigureAwait(false);
    }
}

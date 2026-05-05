using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenGeneratingGroups;

public class WithInvalidCapacity : WhenGeneratingGroups
{
    protected override Task SetupScenarioAsync()
    {
        Command = new GenerateGroupCommand("Group A", 10);

        var availableTeams = new Team[]
        {
            Team.Create(Guid.NewGuid(), "Team 1", 80).Value!,
            Team.Create(Guid.NewGuid(), "Team 2", 75).Value!
        };

        TeamRepository.GetAllAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult<IEnumerable<Team>>(availableTeams));

        return Task.CompletedTask;
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotAddGroupToRepository()
    {
        GroupRepository.DidNotReceive().Add(Arg.Any<Group>());
    }

    [Test]
    public void ShouldNotCallFixtureScheduler()
    {
        FixtureSchedulerService.DidNotReceive().GenerateFixtures(Arg.Any<Group>());
    }
}

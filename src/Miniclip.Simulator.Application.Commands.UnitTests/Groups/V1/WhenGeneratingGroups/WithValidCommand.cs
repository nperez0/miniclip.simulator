using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenGeneratingGroups;

public class WithValidCommand : WhenGeneratingGroups
{
    protected override Task SetupScenarioAsync()
    {
        Command = new GenerateGroupCommand("Group A", 4);

        var availableTeams = new Team[]
        {
            Team.Create(Guid.NewGuid(), "Team 1", 80).Value!,
            Team.Create(Guid.NewGuid(), "Team 2", 75).Value!,
            Team.Create(Guid.NewGuid(), "Team 3", 70).Value!,
            Team.Create(Guid.NewGuid(), "Team 4", 65).Value!,
            Team.Create(Guid.NewGuid(), "Team 5", 60).Value!,
            Team.Create(Guid.NewGuid(), "Team 6", 55).Value!
        };

        TeamRepository.GetAllAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult<IEnumerable<Team>>(availableTeams));

        FixtureSchedulerService.GenerateFixtures(Arg.Any<Group>())
            .Returns(Core.Result.Success());

        return Task.CompletedTask;
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnGroupId()
    {
        Result.Value.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void ShouldAddGroupToRepository()
    {
        GroupRepository.Received(1).Add(Arg.Is<Group>(g =>
            g.Name == "Group A" &&
            g.Capacity == 4));
    }

    [Test]
    public void ShouldSelectCorrectNumberOfTeams()
    {
        GroupRepository.Received(1).Add(Arg.Is<Group>(g =>
            g.Teams.Count() == 4));
    }

    [Test]
    public void ShouldCallFixtureScheduler()
    {
        FixtureSchedulerService.Received(1).GenerateFixtures(Arg.Any<Group>());
    }
}

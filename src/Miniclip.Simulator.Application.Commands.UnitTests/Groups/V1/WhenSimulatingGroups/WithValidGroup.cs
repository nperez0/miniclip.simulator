using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenSimulatingGroups;

public class WithValidGroup : WhenSimulatingGroups
{
    private Guid groupId;
    private Group group = null!;

    protected override Task SetupScenarioAsync()
    {
        groupId = Guid.NewGuid();
        Command = new SimulateGroupCommand(groupId);

        (group, var teams) = GroupMother.WithTeams(4, id: groupId);

        group.AddMatch(Guid.NewGuid(), teams[0], teams[1], 1);
        group.AddMatch(Guid.NewGuid(), teams[2], teams[3], 1);

        GroupRepository.FindAsync(groupId, CancellationToken.None)
            .ReturnsForAnyArgs(group);

        GroupSimulator.SimulateAllMatches(group)
            .Returns(Result.Success());

        return Task.CompletedTask;
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldFindGroupById()
    {
        GroupRepository.Received(1).FindAsync(groupId, Arg.Any<CancellationToken>());
    }

    [Test]
    public void ShouldCallGroupSimulator()
    {
        GroupSimulator.Received(1).SimulateAllMatches(group);
    }
}

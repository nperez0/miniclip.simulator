using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenSimulatingGroups;

public class WithNonExistentGroup : WhenSimulatingGroups
{
    private Guid groupId;

    protected override Task SetupScenarioAsync()
    {
        groupId = Guid.NewGuid();
        Command = new SimulateGroupCommand(groupId);

        GroupRepository.FindAsync(groupId, CancellationToken.None)
            .ReturnsForAnyArgs((Group?)null);

        return Task.CompletedTask;
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotCallGroupSimulator()
    {
        GroupSimulator.DidNotReceive().SimulateAllMatches(Arg.Any<Group>());
    }

    [Test]
    public void ShouldReturnGroupNotFoundError()
    {
        Result.Error.Type.ShouldBe(ErrorType.NotFound);
        Result.Error.Code.ShouldBe(SimulateGroupErrors.GroupNotFoundCode);
    }
}

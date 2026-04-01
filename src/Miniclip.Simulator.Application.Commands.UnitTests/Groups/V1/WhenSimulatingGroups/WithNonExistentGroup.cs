using Shouldly;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenSimulatingGroups;

public class WithNonExistentGroup : WhenSimulatingGroups
{
    private Guid groupId;

    protected override async Task GivenAsync()
    {
        await base.GivenAsync();

        groupId = Guid.NewGuid();
        Command = new SimulateGroupCommand(groupId);

        GroupRepository.FindAsync(groupId, CancellationToken.None)
            .ReturnsForAnyArgs((Group?)null);
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
        Result.Error.Code.ShouldBe("GROUP_NOT_FOUND");
    }
}

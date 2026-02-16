using FluentAssertions;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenSimulatingGroups;

public class WithNonExistentGroup : WhenSimulatingGroups
{
    private Guid groupId;

    protected override void Given()
    {
        base.Given();

        groupId = Guid.NewGuid();
        Command = new SimulateGroupCommand(groupId);

        GroupRepository.FindAsync(groupId, default)
            .ReturnsForAnyArgs((Group?)null);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldNotCallGroupSimulator()
    {
        GroupSimulator.DidNotReceive().SimulateAllMatches(Arg.Any<Group>());
    }

    [Test]
    public void ShouldReturnGroupNotFoundException()
    {
        Result.Exception.Should().BeOfType<GroupNotFoundException>();
    }
}

using FluentAssertions;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenSimulatingGroups;

public class WithValidGroup : WhenSimulatingGroups
{
    private Guid groupId;
    private Group group = null!;

    protected override void Given()
    {
        base.Given();

        groupId = Guid.NewGuid();
        Command = new SimulateGroupCommand(groupId);

        // Create a group with teams and matches
        group = Group.Create(groupId, "Group A", 4).Value!;
        
        var team1 = Team.Create(Guid.NewGuid(), "Team 1", 80).Value!;
        var team2 = Team.Create(Guid.NewGuid(), "Team 2", 75).Value!;
        var team3 = Team.Create(Guid.NewGuid(), "Team 3", 70).Value!;
        var team4 = Team.Create(Guid.NewGuid(), "Team 4", 65).Value!;

        group.AddTeam(team1);
        group.AddTeam(team2);
        group.AddTeam(team3);
        group.AddTeam(team4);

        // Add unplayed matches
        group.AddMatch(Guid.NewGuid(), team1, team2, 1);
        group.AddMatch(Guid.NewGuid(), team3, team4, 1);

        GroupRepository.FindAsync(groupId, default)
            .ReturnsForAnyArgs(group);

        GroupSimulator.SimulateAllMatches(group)
            .Returns(Result.Success());
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.IsSuccess.Should().BeTrue();
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

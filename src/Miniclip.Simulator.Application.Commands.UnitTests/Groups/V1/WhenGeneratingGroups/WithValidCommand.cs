using FluentAssertions;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenGeneratingGroups;

public class WithValidCommand : WhenGeneratingGroups
{
    protected override void Given()
    {
        base.Given();

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

        TeamRepository.GetAllAsync(default)
            .ReturnsForAnyArgs(Task.FromResult<IEnumerable<Team>>(availableTeams));

        FixtureSchedulerService.GenerateFixtures(Arg.Any<Group>())
            .Returns(Core.Result.Success());
    }

    [Test]
    public void ShouldReturnSuccess()
    {
        Result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnGroupId()
    {
        Result.Value.Should().NotBeEmpty();
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

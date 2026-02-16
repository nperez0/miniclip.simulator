using FluentAssertions;
using Miniclip.Core;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Application.Commands.UnitTests.Groups.V1.WhenGeneratingGroups;

public class WithInvalidCapacity : WhenGeneratingGroups
{
    protected override void Given()
    {
        base.Given();

        Command = new GenerateGroupCommand("Group A", 10);

        var availableTeams = new List<Team>
        {
            Team.Create(Guid.NewGuid(), "Team 1", 80).Value!,
            Team.Create(Guid.NewGuid(), "Team 2", 75).Value!
        };

        TeamRepository.GetAllAsync(default)
            .ReturnsForAnyArgs(Task.FromResult<IEnumerable<Team>>(availableTeams));
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.IsFailure.Should().BeTrue();
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

using FluentAssertions;
using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WithInsufficientTeams : WhenGeneratingFixtures
{
    protected override void Given()
    {
        Capacity = 4;

        Group = Group.Create(Guid.NewGuid(), "Group A", Capacity).Value;
        Group!.AddTeam(Team.Create(Guid.NewGuid(), "Team 1", 50).Value!);
        Group!.AddTeam(Team.Create(Guid.NewGuid(), "Team 2", 60).Value!);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldReturnInvalidTeamCountException()
    {
        Result!.Exception.Should().BeOfType<GroupGenerateFixturesException>();
        Result!.Exception.Message.Should().Contain("must have exactly");
    }

    [Test]
    public void ShouldNotGenerateAnyMatches()
    {
        Group!.Matches.Should().BeEmpty();
    }
}

using FluentAssertions;
using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WithNoTeams : WhenGeneratingFixtures
{
    protected override void Given()
    {
        Capacity = 4;

        Group = Group.Create(Guid.NewGuid(), "Group A", Capacity).Value;
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
        Result!.Exception.Message.Should().Contain("Current count: 0");
    }

    [Test]
    public void ShouldNotGenerateAnyMatches()
    {
        Group!.Matches.Should().BeEmpty();
    }
}

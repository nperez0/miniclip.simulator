using FluentAssertions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WithInsufficientTeams : WhenGeneratingFixtures
{
    protected override void Given()
    {
        base.Given();

        Capacity = 4;

        GivenGroupWithTeams(2);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.Should().NotBeNull();
        Result!.IsFailure.Should().BeTrue();
    }

    [Test]
    public void ShouldNotCallFixtureSchedulerFactory()
    {
        FixtureSchedulerFactory!.DidNotReceive().Create(Arg.Any<Group>());
    }

    [Test]
    public void ShouldNotCallSchedulerGenerateSchedule()
    {
        FixtureScheduler!.DidNotReceive().GenerateSchedule();
    }

    [Test]
    public void ShouldIndicateExpectedAndActualTeamCount()
    {
        Result!.Exception.Should().BeOfType<GroupGenerateFixturesException>();
        Result!.Exception.Message.Should().Contain("4");
        Result!.Exception.Message.Should().Contain("2");
    }

    [Test]
    public void ShouldNotGenerateAnyMatches()
    {
        Group!.Matches.Should().BeEmpty();
    }
}

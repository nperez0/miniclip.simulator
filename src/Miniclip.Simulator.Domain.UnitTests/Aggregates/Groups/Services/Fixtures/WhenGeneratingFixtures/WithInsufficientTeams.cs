using Shouldly;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WithInsufficientTeams : WhenGeneratingFixtures
{
    protected override void Given()
    {
        base.Given();

        Capacity = 4;

        (Group, _) = GroupMother.WithTeams(2, Capacity);
    }

    [Test]
    public void ShouldReturnFailure()
    {
        Result.ShouldNotBeNull();
        Result!.IsFailure.ShouldBeTrue();
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
        Result!.Error.Code.ShouldBe("GROUP_INVALID_TEAM_COUNT");
        Result!.Error.Message.ShouldBe(GroupGenerateFixturesErrors.InvalidTeamCount(Capacity, Group!.Teams.Count).Message);
    }

    [Test]
    public void ShouldNotGenerateAnyMatches()
    {
        Group!.Matches.ShouldBeEmpty();
    }
}

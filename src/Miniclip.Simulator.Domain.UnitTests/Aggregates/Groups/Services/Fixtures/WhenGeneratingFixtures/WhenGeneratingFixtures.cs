using AutoFixture;
using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Fixtures.WhenGeneratingFixtures;

public class WhenGeneratingFixtures : TestBase<FixtureSchedulerService>
{
    protected IFixtureSchedulerFactory? FixtureSchedulerFactory { get; set; }

    protected IFixtureScheduler? FixtureScheduler { get; set; }

    protected int Capacity { get; set; }

    protected Group? Group { get; set; }

    protected Result? Result { get; set; }

    protected override void Given()
    {
        FixtureSchedulerFactory = Fixture.Freeze<IFixtureSchedulerFactory>();
        FixtureScheduler = Fixture.Freeze<IFixtureScheduler>();

        FixtureSchedulerFactory!.Create(Arg.Any<Group>()).Returns(FixtureScheduler);
    }

    protected override void When()
    {
        Result = Sut!.GenerateFixtures(Group!);
    }

    protected Team[] GivenGroupWithTeams(int count)
    {
        var teams = new Team[count];

        Group = Group.Create(Guid.NewGuid(), $"Group A", 4).Value!;

        for (int i = 0; i < count; i++)
        {
            teams[i] = Team.Create(Guid.NewGuid(), $"Team {i + 1}", 50 + i * 10).Value!;
            Group!.AddTeam(teams[i]);
        }

        return teams;
    }
}

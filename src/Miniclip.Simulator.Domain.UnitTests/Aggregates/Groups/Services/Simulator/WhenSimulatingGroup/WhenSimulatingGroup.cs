using AutoFixture;
using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using NSubstitute;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WhenSimulatingGroup : TestBase<GroupSimulator>
{
    protected IMatchSimulatorFactory? MatchSimulatorFactory { get; set; }

    protected IMatchSimulator? MatchSimulator { get; set; }

    protected Group? Group { get; set; }

    protected Result? Result { get; private set; }

    protected override void Given()
    {
        MatchSimulatorFactory = Fixture.Freeze<IMatchSimulatorFactory>();
        MatchSimulator = Fixture.Freeze<IMatchSimulator>();

        MatchSimulatorFactory!.Create(Arg.Any<Group>()).Returns(MatchSimulator);
    }

    protected override void When()
    {
        Result = Sut!.SimulateAllMatches(Group!);
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

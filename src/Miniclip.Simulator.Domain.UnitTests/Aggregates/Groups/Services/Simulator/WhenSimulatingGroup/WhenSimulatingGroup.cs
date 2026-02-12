using AutoFixture;
using Miniclip.Core;
using Miniclip.Core.Tests;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Aggregates.Groups.Services.Simulator.WhenSimulatingGroup;

public class WhenSimulatingGroup : TestBase<GroupSimulator>
{
    protected IMatchSimulator? MatchSimulator { get; private set; }

    protected List<Team> Teams { get; set; }

    protected Group? Group { get; set; }

    protected Result? Result { get; private set; }

    protected override void When()
    {
        Result = Sut!.SimulateAllMatches(Group!);
    }

    protected void AssumeExistingGroupWithTeamsAndGeneratedFixtures()
    {
        AssumeExistingGroupWithTeams();
        AssumeGenerateFixtures();
    }

    protected void AssumeExistingGroupWithTeams()
    {
        MatchSimulator = Fixture.Freeze<IMatchSimulator>();

        Group = Group.Create(Guid.NewGuid(), "Group A", Teams.Count).Value!;

        foreach (var team in Teams)
            Group.AddTeam(team);
    }

    protected void AssumeGenerateFixtures()
    {
        new FixtureSchedulerService().GenerateFixtures(Group!);
    }

    protected void AssumeSimulateResult()
    {
        foreach (var match in Group!.Matches)
            match.SimulateResult(2, 1);
    }
}

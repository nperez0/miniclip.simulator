using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.ReadModels.Models;
using NUnit.Framework;
using Shouldly;

namespace Miniclip.Simulator.ReadModels.Projections.IntegrationTests.WhenAMatchIsPlayed;

[TestFixture]
public class WithSubsequentMatchSameGroup : WhenAMatchIsPlayed
{
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _teamAId = Guid.NewGuid();
    private readonly Guid _teamBId = Guid.NewGuid();
    private readonly Guid _teamCId = Guid.NewGuid();

    protected override IReadOnlyList<MatchPlayed> Events =>
    [
        new MatchPlayed(
            GroupId: _groupId,
            GroupName: "Group A",
            MatchId: Guid.NewGuid(),
            HomeTeamId: _teamAId,
            HomeTeamName: "Team A",
            HomeTeamStrength: 80,
            HomeScore: 2,
            AwayTeamId: _teamBId,
            AwayTeamName: "Team B",
            AwayTeamStrength: 70,
            AwayScore: 1,
            Round: 1),

        new MatchPlayed(
            GroupId: _groupId,
            GroupName: "Group A",
            MatchId: Guid.NewGuid(),
            HomeTeamId: _teamCId,
            HomeTeamName: "Team C",
            HomeTeamStrength: 85,
            HomeScore: 3,
            AwayTeamId: _teamAId,
            AwayTeamName: "Team A",
            AwayTeamStrength: 80,
            AwayScore: 3,
            Round: 2)
    ];

    [Test]
    public async Task ShouldCreateTwoMatchResultRows()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var count = await context.Set<MatchResultModel>()
            .CountAsync(m => m.GroupId == _groupId);

        count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldAccumulateTeamAStats()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var standing = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == _teamAId);

        standing.MatchesPlayed.ShouldBe(2);
        standing.Wins.ShouldBe(1);
        standing.Draws.ShouldBe(1);
        standing.Points.ShouldBe(4);
    }

    [Test]
    public async Task ShouldRankTeamCFirstAfterBigWin()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var teamC = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == _teamCId);
        var teamA = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == _teamAId);

        teamC.Points.ShouldBe(1);
        teamA.Points.ShouldBe(4);
        teamA.Position.ShouldBe(1);
    }
}

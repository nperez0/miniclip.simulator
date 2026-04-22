using Microsoft.Extensions.DependencyInjection;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.IntegrationEvents.V1;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Projections.IntegrationTests.WhenAMatchIsPlayed;

[TestFixture]
public class WithSubsequentMatchSameGroup : WhenAMatchIsPlayed
{
    private readonly Guid groupId = Guid.NewGuid();
    private readonly Guid teamAId = Guid.NewGuid();
    private readonly Guid teamBId = Guid.NewGuid();
    private readonly Guid teamCId = Guid.NewGuid();

    protected override IReadOnlyList<MatchPlayedIntegrationEvent> Events =>
    [
        new(
            GroupId: groupId,
            GroupName: "Group A",
            MatchId: Guid.NewGuid(),
            HomeTeamId: teamAId,
            HomeTeamName: "Team A",
            HomeTeamStrength: 80,
            HomeScore: 2,
            AwayTeamId: teamBId,
            AwayTeamName: "Team B",
            AwayTeamStrength: 70,
            AwayScore: 1,
            Round: 1),

        new(
            GroupId: groupId,
            GroupName: "Group A",
            MatchId: Guid.NewGuid(),
            HomeTeamId: teamCId,
            HomeTeamName: "Team C",
            HomeTeamStrength: 85,
            HomeScore: 3,
            AwayTeamId: teamAId,
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
            .CountAsync(m => m.GroupId == groupId);

        count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldAccumulateTeamAStats()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var standing = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == teamAId);

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
            .FirstAsync(s => s.TeamId == teamCId);
        var teamA = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == teamAId);

        teamC.Points.ShouldBe(1);
        teamA.Points.ShouldBe(4);
        teamA.Position.ShouldBe(1);
    }
}

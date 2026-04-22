using Microsoft.Extensions.DependencyInjection;
using Miniclip.Simulator.Infrastructure.Read.Persistence;
using Miniclip.Simulator.IntegrationEvents.V1;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Projections.IntegrationTests.WhenAMatchIsPlayed;

[TestFixture]
public class WithFirstMatchInGroup : WhenAMatchIsPlayed
{
    private readonly Guid groupId = Guid.NewGuid();
    private readonly Guid homeTeamId = Guid.NewGuid();
    private readonly Guid awayTeamId = Guid.NewGuid();

    protected override IReadOnlyList<MatchPlayedIntegrationEvent> Events =>
    [
        new(
            GroupId: groupId,
            GroupName: "Group A",
            MatchId: Guid.NewGuid(),
            HomeTeamId: homeTeamId,
            HomeTeamName: "Team A",
            HomeTeamStrength: 80,
            HomeScore: 2,
            AwayTeamId: awayTeamId,
            AwayTeamName: "Team B",
            AwayTeamStrength: 70,
            AwayScore: 1,
            Round: 1)
    ];

    [Test]
    public async Task ShouldCreateMatchResultRow()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var result = await context.Set<MatchResultModel>()
            .FirstOrDefaultAsync(m => m.GroupId == groupId);

        result.ShouldNotBeNull();
        result.HomeTeamId.ShouldBe(homeTeamId);
        result.AwayTeamId.ShouldBe(awayTeamId);
        result.HomeScore.ShouldBe(2);
        result.AwayScore.ShouldBe(1);
    }

    [Test]
    public async Task ShouldCreateStandingsForBothTeams()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var standings = await context.Set<GroupStandingsModel>()
            .Where(s => s.GroupId == groupId)
            .ToListAsync();

        standings.Count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldRecordHomeTeamWin()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var standing = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == homeTeamId);

        standing.Wins.ShouldBe(1);
        standing.Losses.ShouldBe(0);
        standing.GoalsFor.ShouldBe(2);
        standing.GoalsAgainst.ShouldBe(1);
        standing.Points.ShouldBe(3);
    }

    [Test]
    public async Task ShouldRecordAwayTeamLoss()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var standing = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == awayTeamId);

        standing.Wins.ShouldBe(0);
        standing.Losses.ShouldBe(1);
        standing.GoalsFor.ShouldBe(1);
        standing.GoalsAgainst.ShouldBe(2);
        standing.Points.ShouldBe(0);
    }

    [Test]
    public async Task ShouldAssignHomeTeamFirstPosition()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorReadDbContext>();
        var standing = await context.Set<GroupStandingsModel>()
            .FirstAsync(s => s.TeamId == homeTeamId);

        standing.Position.ShouldBe(1);
    }
}

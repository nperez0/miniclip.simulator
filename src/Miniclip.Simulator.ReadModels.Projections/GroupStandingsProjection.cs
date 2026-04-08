using Miniclip.Core.ReadModels.Projections;
using Miniclip.Core.ReadModels.Projections.Attributes;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Projections.Services;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections;

[HandlerHighPriority(2)]
public class GroupStandingsProjection(
    IGroupStandingsRepository groupStandingsRepository,
    IRecalculatePositionService recalculatePositionService)
    : IProjectionHandler<MatchPlayed>
{
    public async ValueTask HandleAsync(MatchPlayed @event, CancellationToken cancellationToken)
    {

        var allStandings = await GetCurrentStandings(@event.GroupId, cancellationToken);

        // Get or create standings for both teams
        var homeTeamStanding = GetOrCreateStanding(
            allStandings,
            @event.GroupId,
            @event.GroupName,
            @event.HomeTeamId,
            @event.HomeTeamName,
            @event.HomeTeamStrength);
        var awayTeamStanding = GetOrCreateStanding(
            allStandings,
            @event.GroupId,
            @event.GroupName,
            @event.AwayTeamId,
            @event.AwayTeamName,
            @event.AwayTeamStrength);

        // Update statistics
        UpdateTeamStanding(homeTeamStanding, @event.HomeScore, @event.AwayScore);
        UpdateTeamStanding(awayTeamStanding, @event.AwayScore, @event.HomeScore);

        // Save new standings if they were created
        if (allStandings.TryAdd(@event.HomeTeamId, homeTeamStanding))
            groupStandingsRepository.Add(homeTeamStanding);

        if (allStandings.TryAdd(@event.AwayTeamId, awayTeamStanding))
            groupStandingsRepository.Add(awayTeamStanding);

        // Recalculate positions for all teams in group (with H2H logic)
        await recalculatePositionService.RecalculatePositionsAsync(allStandings.Values, @event.GroupId, cancellationToken);
    }

    private async Task<Dictionary<Guid, GroupStandingsModel>> GetCurrentStandings(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var standings = await groupStandingsRepository.GetStandingsByGroupIdAsync(groupId, cancellationToken);

        return standings.ToDictionary(s => s.TeamId);
    }

    private static GroupStandingsModel GetOrCreateStanding(
        Dictionary<Guid, GroupStandingsModel> allStandings,
        Guid groupId,
        string groupName,
        Guid teamId,
        string teamName,
        int teamStrength)
    {
        if (allStandings.TryGetValue(teamId, out var existing))
            return existing;

        return new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            GroupName = groupName,
            TeamId = teamId,
            TeamName = teamName,
            TeamStrength = teamStrength,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static void UpdateTeamStanding(
        GroupStandingsModel standing,
        int goalsFor,
        int goalsAgainst)
    {
        standing.MatchesPlayed++;
        standing.GoalsFor += goalsFor;
        standing.GoalsAgainst += goalsAgainst;
        standing.GoalDifference = standing.GoalsFor - standing.GoalsAgainst;

        if (goalsFor > goalsAgainst)
        {
            standing.Wins++;
            standing.Points += 3;
        }
        else if (goalsFor == goalsAgainst)
        {
            standing.Draws++;
            standing.Points += 1;
        }
        else
            standing.Losses++;

        standing.LastUpdated = DateTime.UtcNow;
    }


}

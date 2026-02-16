using Miniclip.Simulator.ReadModels.Models;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections.Services;

public class RecalculatePositionService(IMatchResultsRepository matchResultsRepository) 
    : IRecalculatePositionService
{
    public async Task RecalculatePositionsAsync(
        IEnumerable<GroupStandingsModel> standings,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        // Initial sort by overall stats
        var sorted = standings
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenByDescending(s => s.GoalsFor)
            .ThenBy(s => s.GoalsAgainst)
            .ToArray();

        // Find groups of teams tied on all criteria
        var tiedGroups = sorted
            .Select((standing, index) => (standing, index))
            .GroupBy(x => new
            {
                x.standing.Points,
                x.standing.GoalDifference,
                x.standing.GoalsFor,
                x.standing.GoalsAgainst
            })
            .Where(g => g.Count() > 1)
            .ToArray();

        // If there are tied teams, apply head-to-head
        if (tiedGroups.Length != 0)
        {
            var allMatches = await matchResultsRepository.GetMatchResultsByGroupIdAsync(groupId, cancellationToken);

            foreach (var tiedGroup in tiedGroups)
                ApplyHeadToHeadTieBreaker(sorted, [.. tiedGroup], [.. allMatches]);
        }

        // Assign final positions
        for (int i = 0; i < sorted.Length; i++)
        {
            sorted[i].Position = i + 1;
            sorted[i].QualifiesForKnockout = i < 2;
            sorted[i].LastUpdated = DateTime.UtcNow;
        }
    }

    private static void ApplyHeadToHeadTieBreaker(
        GroupStandingsModel[] currentStandings,
        (GroupStandingsModel standing, int index)[] tiedTeams,
        MatchResultModel[] allMatches)
    {
        var tiedTeamIds = tiedTeams.Select(t => t.standing.TeamId).ToHashSet();

        // Get only matches between tied teams
        var h2hMatches = allMatches
            .Where(m => m.IsPlayed &&
                       tiedTeamIds.Contains(m.HomeTeamId) &&
                       tiedTeamIds.Contains(m.AwayTeamId))
            .ToArray();

        // If no head-to-head matches exist yet, can't apply H2H
        if (h2hMatches.Length == 0)
            return;

        // Calculate head-to-head mini-table
        var h2hStats = CalculateHeadToHeadStats(tiedTeams.Select(t => t.standing), h2hMatches);

        // Sort by head-to-head criteria
        var h2hSorted = h2hStats
            .OrderByDescending(s => s.H2HPoints)
            .ThenByDescending(s => s.H2HGoalDifference)
            .ThenByDescending(s => s.H2HGoalsFor)
            .ThenBy(s => s.H2HGoalsAgainst)
            .ThenBy(s => s.TeamName)
            .ToArray();

        // Replace tied teams in original list with H2H sorted order
        var minIndex = tiedTeams.Min(t => t.index);
        for (int i = 0; i < h2hSorted.Length; i++)
        {
            var originalStanding = tiedTeams.First(t => t.standing.TeamId == h2hSorted[i].TeamId).standing;
            currentStandings[minIndex + i] = originalStanding;
        }
    }

    private static List<HeadToHeadStats> CalculateHeadToHeadStats(
        IEnumerable<GroupStandingsModel> tiedTeams,
        MatchResultModel[] h2hMatches)
    {
        var stats = new List<HeadToHeadStats>();

        foreach (var team in tiedTeams)
        {
            var homeMatches = h2hMatches.Where(m => m.HomeTeamId == team.TeamId).ToArray();
            var awayMatches = h2hMatches.Where(m => m.AwayTeamId == team.TeamId).ToArray();

            var h2hWins = homeMatches.Count(m => m.HomeScore > m.AwayScore) +
                         awayMatches.Count(m => m.AwayScore > m.HomeScore);

            var h2hDraws = homeMatches.Count(m => m.HomeScore == m.AwayScore) +
                          awayMatches.Count(m => m.AwayScore == m.HomeScore);

            var h2hGoalsFor = homeMatches.Sum(m => m.HomeScore) +
                             awayMatches.Sum(m => m.AwayScore);

            var h2hGoalsAgainst = homeMatches.Sum(m => m.AwayScore) +
                                 awayMatches.Sum(m => m.HomeScore);

            stats.Add(new HeadToHeadStats
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                H2HPoints = (h2hWins * 3) + h2hDraws,
                H2HGoalsFor = h2hGoalsFor,
                H2HGoalsAgainst = h2hGoalsAgainst,
                H2HGoalDifference = h2hGoalsFor - h2hGoalsAgainst
            });
        }

        return stats;
    }

    private record HeadToHeadStats
    {
        public Guid TeamId { get; init; }
        public string TeamName { get; init; } = string.Empty;
        public int H2HPoints { get; init; }
        public int H2HGoalDifference { get; init; }
        public int H2HGoalsFor { get; init; }
        public int H2HGoalsAgainst { get; init; }
    }
}

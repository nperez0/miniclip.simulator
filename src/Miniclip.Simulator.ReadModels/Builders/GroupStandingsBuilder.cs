using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Builders;

/// <summary>
/// Builds denormalized read models from domain aggregates.
/// Called by projection handlers after domain events are processed.
/// </summary>
public class GroupStandingsBuilder
{
    /// <summary>
    /// Build complete standings for a group from domain entities.
    /// Returns a flat list of team standings with positions and qualifications.
    /// </summary>
    public List<GroupStandingsModel> BuildStandings(Group group)
    {
        var teamStats = CalculateTeamStatistics(group);
        var sortedStandings = SortByTournamentRules(teamStats);
        
        return sortedStandings.Select((stat, index) => new GroupStandingsModel
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            GroupName = group.Name,
            Position = index + 1,
            TeamId = stat.TeamId,
            TeamName = stat.TeamName,
            TeamStrength = stat.TeamStrength,
            MatchesPlayed = stat.MatchesPlayed,
            Wins = stat.Wins,
            Draws = stat.Draws,
            Losses = stat.Losses,
            GoalsFor = stat.GoalsFor,
            GoalsAgainst = stat.GoalsAgainst,
            GoalDifference = stat.GoalDifference,
            Points = stat.Points,
            QualifiesForKnockout = index < 2, // Top 2 qualify
            LastUpdated = DateTime.UtcNow
        }).ToList();
    }

    private List<TeamStatistic> CalculateTeamStatistics(Group group)
    {
        return group.Teams.Select(team => 
        {
            var homeMatches = group.Matches.Where(m => m.IsPlayed && m.HomeTeam.Id == team.Id).ToList();
            var awayMatches = group.Matches.Where(m => m.IsPlayed && m.AwayTeam.Id == team.Id).ToList();

            var homeWins = homeMatches.Count(m => m.HomeScore > m.AwayScore);
            var awayWins = awayMatches.Count(m => m.AwayScore > m.HomeScore);
            var homeDraws = homeMatches.Count(m => m.HomeScore == m.AwayScore);
            var awayDraws = awayMatches.Count(m => m.AwayScore == m.HomeScore);
            var homeLosses = homeMatches.Count(m => m.HomeScore < m.AwayScore);
            var awayLosses = awayMatches.Count(m => m.AwayScore < m.HomeScore);

            var goalsFor = homeMatches.Sum(m => m.HomeScore) + awayMatches.Sum(m => m.AwayScore);
            var goalsAgainst = homeMatches.Sum(m => m.AwayScore) + awayMatches.Sum(m => m.HomeScore);

            return new TeamStatistic
            {
                TeamId = team.Id,
                TeamName = team.Name,
                TeamStrength = team.Strength,
                MatchesPlayed = homeMatches.Count + awayMatches.Count,
                Wins = homeWins + awayWins,
                Draws = homeDraws + awayDraws,
                Losses = homeLosses + awayLosses,
                GoalsFor = goalsFor,
                GoalsAgainst = goalsAgainst,
                GoalDifference = goalsFor - goalsAgainst,
                Points = (homeWins + awayWins) * 3 + (homeDraws + awayDraws)
            };
        }).ToList();
    }

    private List<TeamStatistic> SortByTournamentRules(List<TeamStatistic> stats)
    {
        // Sort according to tournament rules:
        // 1. Points
        // 2. Goal difference
        // 3. Goals for
        // 4. Goals against
        return stats
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.GoalDifference)
            .ThenByDescending(x => x.GoalsFor)
            .ThenBy(x => x.GoalsAgainst)
            .ToList();
    }

    private class TeamStatistic
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int TeamStrength { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference { get; set; }
        public int Points { get; set; }
    }
}

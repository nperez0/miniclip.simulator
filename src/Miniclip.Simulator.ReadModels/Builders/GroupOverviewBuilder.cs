using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.ReadModels.Builders;

/// <summary>
/// Builds complete group overview read model from domain entities.
/// Combines standings and match results into a single view.
/// </summary>
public class GroupOverviewBuilder
{
    /// <summary>
    /// Build a complete group overview with embedded standings and matches.
    /// </summary>
    public GroupOverviewReadModel BuildOverview(Group group)
    {
        var standings = BuildStandingItems(group);
        var matches = BuildMatchItems(group);

        var playedMatches = group.Matches.Count(m => m.IsPlayed);
        var totalMatches = group.Matches.Count;

        return new GroupOverviewReadModel
        {
            GroupId = group.Id,
            GroupName = group.Name,
            Capacity = group.Capacity,
            TotalTeams = group.Teams.Count,
            TotalMatches = totalMatches,
            PlayedMatches = playedMatches,
            RemainingMatches = totalMatches - playedMatches,
            IsComplete = playedMatches == totalMatches && totalMatches > 0,
            Standings = standings,
            RecentMatches = matches,
            LastUpdated = DateTime.UtcNow,
        };
    }

    private List<TeamStandingItem> BuildStandingItems(Group group)
    {
        var teamStats = group.Teams.Select(team => 
        {
            var homeMatches = group.Matches.Where(m => m.IsPlayed && m.HomeTeam.Id == team.Id).ToList();
            var awayMatches = group.Matches.Where(m => m.IsPlayed && m.AwayTeam.Id == team.Id).ToList();

            var wins = homeMatches.Count(m => m.HomeScore > m.AwayScore) + 
                      awayMatches.Count(m => m.AwayScore > m.HomeScore);
            var draws = homeMatches.Count(m => m.HomeScore == m.AwayScore) + 
                       awayMatches.Count(m => m.AwayScore == m.HomeScore);
            var losses = homeMatches.Count(m => m.HomeScore < m.AwayScore) + 
                        awayMatches.Count(m => m.AwayScore < m.HomeScore);
            var goalsFor = homeMatches.Sum(m => m.HomeScore) + awayMatches.Sum(m => m.AwayScore);
            var goalsAgainst = homeMatches.Sum(m => m.AwayScore) + awayMatches.Sum(m => m.HomeScore);

            return new
            {
                TeamId = team.Id,
                TeamName = team.Name,
                MatchesPlayed = homeMatches.Count + awayMatches.Count,
                Wins = wins,
                Draws = draws,
                Losses = losses,
                GoalsFor = goalsFor,
                GoalsAgainst = goalsAgainst,
                GoalDifference = goalsFor - goalsAgainst,
                Points = wins * 3 + draws
            };
        })
        .OrderByDescending(x => x.Points)
        .ThenByDescending(x => x.GoalDifference)
        .ThenByDescending(x => x.GoalsFor)
        .ThenBy(x => x.GoalsAgainst)
        .ToList();

        return teamStats.Select((stat, index) => new TeamStandingItem
        {
            Position = index + 1,
            TeamId = stat.TeamId,
            TeamName = stat.TeamName,
            Points = stat.Points,
            MatchesPlayed = stat.MatchesPlayed,
            Wins = stat.Wins,
            Draws = stat.Draws,
            Losses = stat.Losses,
            GoalsFor = stat.GoalsFor,
            GoalsAgainst = stat.GoalsAgainst,
            GoalDifference = stat.GoalDifference,
            Qualifies = index < 2 // Top 2 qualify
        }).ToList();
    }

    private List<MatchResultItem> BuildMatchItems(Group group)
    {
        return group.Matches
            .OrderByDescending(m => m.IsPlayed)
            .ThenBy(m => m.Round)
            .Select(m => new MatchResultItem
            {
                MatchId = m.Id,
                HomeTeam = m.HomeTeam.Name,
                AwayTeam = m.AwayTeam.Name,
                HomeScore = m.HomeScore,
                AwayScore = m.AwayScore,
                Round = m.Round,
                IsPlayed = m.IsPlayed
            })
            .ToList();
    }
}

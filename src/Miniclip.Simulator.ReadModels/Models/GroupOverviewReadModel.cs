namespace Miniclip.Simulator.ReadModels.Models;

/// <summary>
/// Complete group overview for dashboard - all data in one model!
/// Perfect for single-query UI rendering.
/// </summary>
public class GroupOverviewReadModel
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int TotalTeams { get; set; }
    public int TotalMatches { get; set; }
    public int PlayedMatches { get; set; }
    public int RemainingMatches { get; set; }
    public bool IsComplete { get; set; }
    
    // Embedded standings (no separate query needed!)
    public List<TeamStandingItem> Standings { get; set; } = new();
    
    // Embedded recent matches (no separate query needed!)
    public List<MatchResultItem> RecentMatches { get; set; } = new();
    
    // Metadata
    public DateTime LastUpdated { get; set; }
    public long Version { get; set; }
}

public class TeamStandingItem
{
    public int Position { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int Points { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference { get; set; }
    public bool Qualifies { get; set; }
}

public class MatchResultItem
{
    public Guid MatchId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int Round { get; set; }
    public bool IsPlayed { get; set; }
}

namespace Miniclip.Simulator.ReadModels.Models;

/// <summary>
/// Denormalized read model optimized for fast queries.
/// Pre-calculated team standings - no joins or calculations needed at query time.
/// </summary>
public class GroupStandingsReadModel
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Flattened team standings - one row per team per group
    public int Position { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int TeamStrength { get; set; }
    
    // Pre-calculated statistics (no computation at query time!)
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference { get; set; }
    public int Points { get; set; }
    public bool QualifiesForKnockout { get; set; }
    
    // Metadata
    public DateTime LastUpdated { get; set; }
    public long Version { get; set; }
}

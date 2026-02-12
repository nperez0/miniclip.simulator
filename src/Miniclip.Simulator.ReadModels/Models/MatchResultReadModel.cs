namespace Miniclip.Simulator.ReadModels.Models;

/// <summary>
/// Denormalized match results with team names embedded.
/// No joins with Teams table needed at query time!
/// </summary>
public class MatchResultReadModel
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Embedded team information (denormalized - no joins!)
    public Guid HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public int HomeTeamStrength { get; set; }
    
    public Guid AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = string.Empty;
    public int AwayTeamStrength { get; set; }
    
    // Match result
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int Round { get; set; }
    public bool IsPlayed { get; set; }
    public DateTime? PlayedAt { get; set; }
    
    // Pre-calculated derived information
    public string Result { get; set; } = string.Empty; // "Home Win", "Draw", "Away Win"
    public int GoalDifference { get; set; }
}

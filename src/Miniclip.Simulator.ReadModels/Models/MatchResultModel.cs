namespace Miniclip.Simulator.ReadModels.Models;

public class MatchResultModel
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Match details
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public bool IsPlayed { get; set; }
    
    // Home team
    public Guid HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    
    // Away team
    public Guid AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = string.Empty;
    public int AwayScore { get; set; }
    
    // Metadata
    public DateTime? PlayedAt { get; set; }
}

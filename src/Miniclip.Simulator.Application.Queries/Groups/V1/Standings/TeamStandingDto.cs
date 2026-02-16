namespace Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

public class TeamStandingDto
{
    public int Position { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int TeamStrength { get; set; }
    
    // Statistics
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference { get; set; }
    public int Points { get; set; }
    public bool QualifiesForKnockout { get; set; }
}

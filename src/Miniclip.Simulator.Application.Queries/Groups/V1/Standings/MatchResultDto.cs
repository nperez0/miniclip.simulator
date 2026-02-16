namespace Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

public class MatchResultDto
{
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public string AwayTeamName { get; set; } = string.Empty;
    public int AwayScore { get; set; }
    public DateTime? PlayedAt { get; set; }
}

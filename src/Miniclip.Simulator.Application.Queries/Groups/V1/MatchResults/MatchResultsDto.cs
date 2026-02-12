using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.MatchResults;

public class MatchResultsDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<MatchResultReadModel> Matches { get; set; } = new();
    
    public Dictionary<int, List<MatchResultReadModel>> MatchesByRound =>
        Matches.GroupBy(m => m.Round)
            .ToDictionary(g => g.Key, g => g.ToList());
}

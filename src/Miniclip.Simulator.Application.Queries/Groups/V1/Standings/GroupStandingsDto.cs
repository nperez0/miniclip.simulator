using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

public class GroupStandingsDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<GroupStandingsModel> Standings { get; set; } = new();
    
    public List<GroupStandingsModel> QualifiedTeams => 
        Standings.Where(s => s.QualifiesForKnockout).ToList();
}

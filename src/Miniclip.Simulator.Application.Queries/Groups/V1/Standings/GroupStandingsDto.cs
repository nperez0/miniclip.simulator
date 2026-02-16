using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

public class GroupStandingsDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public TeamStandingDto[] Standings { get; set; } = [new()];
    public MatchResultDto[] MatchResults { get; set; } = [];

    public TeamStandingDto[] QualifiedTeams =>
        [.. Standings.Where(s => s.QualifiesForKnockout)];
}

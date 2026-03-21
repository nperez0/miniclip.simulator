using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

public record TeamInfo(Guid Id, string Name, int Strength)
{
    public static readonly TeamInfo Dummy = new(Guid.Empty, "Dummy", 0);

    public static TeamInfo FromTeam(Team team)
        => new(team.Id, team.Name, team.Strength);
}

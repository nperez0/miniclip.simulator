using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Common.Tests.Mothers;

public static class GroupMother
{
    public static Group Default(int capacity = 4) =>
        Group.Create(Guid.NewGuid(), "Group A", capacity).Value!;

    public static (Group Group, Team[] Teams) WithTeams(int count, int capacity = 4, Guid? id = null)
    {
        var group = Group.Create(id ?? Guid.NewGuid(), "Group A", capacity).Value!;
        var teams = TeamMother.Many(count);

        foreach (var team in teams)
            group.AddTeam(team);

        return (group, teams);
    }
}

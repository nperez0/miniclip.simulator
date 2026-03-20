using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.UnitTests.Mothers;

public static class GroupMother
{
    public static (Group Group, Team[] Teams) WithTeams(int count, int capacity = 4)
    {
        var group = Group.Create(Guid.NewGuid(), "Group A", capacity).Value!;
        var teams = new Team[count];

        for (var i = 0; i < count; i++)
        {
            teams[i] = Team.Create(Guid.NewGuid(), $"Team {i + 1}", 50 + i * 10).Value!;
            group.AddTeam(teams[i]);
        }

        return (group, teams);
    }
}

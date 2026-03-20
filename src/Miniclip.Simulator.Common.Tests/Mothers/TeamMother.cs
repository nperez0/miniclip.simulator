using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Common.Tests.Mothers;

public static class TeamMother
{
    public static Team Default() =>
        Team.Create(Guid.NewGuid(), "Team A", 50).Value!;

    public static Team[] Many(int count) =>
        Enumerable.Range(1, count)
            .Select(i => Team.Create(Guid.NewGuid(), $"Team {i}", i * 10).Value!)
            .ToArray();
}

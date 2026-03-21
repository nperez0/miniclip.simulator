using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

namespace Miniclip.Simulator.Common.Tests.Mothers;

public static class TeamInfoMother
{
    public static TeamInfo Default()
        => new(Guid.NewGuid(), "Team A", 50);

    public static TeamInfo[] Many(int count)
        => Enumerable.Range(1, count)
            .Select(i => new TeamInfo(Guid.NewGuid(), $"Team {i}", i * 10))
            .ToArray();
}

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class GroupGenerateFixturesException(string message) : Exception(message)
{
    public static GroupGenerateFixturesException InvalidTeamCount(int capacity, int count)
        => new($"Group must have exactly {capacity} teams to generate fixtures. Current count: {count}.");
}

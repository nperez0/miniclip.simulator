namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class GroupGenerateFixturesException(string message) : ExceptionBase(message)
{
    public static GroupGenerateFixturesException InvalidTeamCount(int capacity, int count)
        => new($"Group must have exactly {capacity} teams to generate fixtures. Current count: {count}.");

    public static GroupGenerateFixturesException SameTeam()
        => new("A team cannot play against itself.");
}

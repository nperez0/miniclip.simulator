using Miniclip.Simulator.Domain.Aggregates.Teams.Exceptions;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class GroupCreationException(string message) : Exception(message)
{
    public static GroupCreationException EmptyName(string? name)
        => new($"Team name '{name}' cannot be empty.");

    public static GroupCreationException InvalidCapacity(int capacity, int min, int max)
        => new($"Group capacity must be between {min} and {max}, but was {capacity}.");
}

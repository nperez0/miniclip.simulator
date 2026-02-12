using Miniclip.Simulator.Domain.Aggregates.Teams.Exceptions;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class GroupCreationException(string message) : Exception(message)
{
    public static GroupCreationException EmptyName(string? name)
        => new($"Team name '{name}' cannot be empty.");

    public static GroupCreationException MinimumCapacity()
        => new($"Group capacity must be at least 2.");
}

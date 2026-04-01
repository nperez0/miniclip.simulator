namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public static class GroupCreationErrors
{
    public static Error EmptyName(string? name)
        => Error.Validation("GROUP_NAME_EMPTY", $"Team name '{name}' cannot be empty.");

    public static Error InvalidCapacity(int capacity, int min, int max)
        => Error.Validation("GROUP_CAPACITY_INVALID", $"Group capacity must be between {min} and {max}, but was {capacity}.");
}

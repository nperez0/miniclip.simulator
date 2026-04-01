namespace Miniclip.Simulator.Domain.Aggregates.Teams.Exceptions;

public static class TeamCreationErrors
{
    public static Error EmptyName(string? name)
        => Error.Validation("TEAM_NAME_EMPTY", $"Team name '{name}' cannot be empty.");

    public static Error InvalidStrength(int strength)
        => Error.Validation("TEAM_STRENGTH_INVALID", $"Strength '{strength}' must be between 0 and 100.");
}

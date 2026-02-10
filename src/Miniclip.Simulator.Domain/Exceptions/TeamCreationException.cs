namespace Miniclip.Simulator.Domain.Exceptions;

public class TeamCreationException : Exception
{
    private TeamCreationException(string message) : base(message)
    {
    }

    public static TeamCreationException EmptyName(string? name)
        => new($"Team name '{name}' cannot be empty.");

    public static TeamCreationException InvalidStrength(int strength)
        => new($"Strength '{strength}' must be between 0 and 100.");
}

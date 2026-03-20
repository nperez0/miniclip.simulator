namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class GroupSimulationException(string message, ExceptionType type = ExceptionType.General) : ExceptionBase(message, type)
{
    public static GroupSimulationException AllMatchesPlayed()
        => new("All matches have already been played.");

    public static GroupSimulationException MatchNotFound(Guid matchId)
        => new($"Match with ID '{matchId}' not found.", ExceptionType.NotFound);

    public static GroupSimulationException NegativeScore()
        => new("Scores cannot be negative.");

    public static GroupSimulationException AlreadyPlayed(Guid matchId)
        => new($"Match '{matchId}' has already been played.");
}

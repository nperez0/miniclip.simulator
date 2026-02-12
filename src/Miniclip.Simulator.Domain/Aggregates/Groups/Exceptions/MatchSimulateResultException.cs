namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class MatchSimulateResultException(string message) : Exception(message)
{
    public static MatchSimulateResultException NegativeScore()
        => new("Scores cannot be negative.");

    public static MatchSimulateResultException AlreadyPlayed(Guid matchId)
        => new($"Match '{matchId}' has already been played.");
}

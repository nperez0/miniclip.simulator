namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class MatchSimulationException(string message) : Exception(message)
{
    public static MatchSimulationException NotFound(Guid matchId)
        => new($"Match {matchId} not found");
}

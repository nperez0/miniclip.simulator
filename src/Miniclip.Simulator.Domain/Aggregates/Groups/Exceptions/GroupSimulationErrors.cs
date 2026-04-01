namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public static class GroupSimulationErrors
{
    public static Error AllMatchesPlayed()
        => Error.Validation("GROUP_ALL_MATCHES_PLAYED", "All matches have already been played.");

    public static Error MatchNotFound(Guid matchId)
        => Error.NotFound("GROUP_MATCH_NOT_FOUND", $"Match with ID '{matchId}' not found.");

    public static Error NegativeScore()
        => Error.Validation("GROUP_NEGATIVE_SCORE", "Scores cannot be negative.");

    public static Error AlreadyPlayed(Guid matchId)
        => Error.Validation("GROUP_MATCH_ALREADY_PLAYED", $"Match '{matchId}' has already been played.");
}

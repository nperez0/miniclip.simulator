namespace Miniclip.Simulator.Domain.Aggregates.Groups.Errors;

public static class GroupSimulationErrors
{
    public const string AllMatchesPlayedCode = "GROUP_ALL_MATCHES_PLAYED";
    public const string MatchNotFoundCode = "GROUP_MATCH_NOT_FOUND";
    public const string NegativeScoreCode = "GROUP_NEGATIVE_SCORE";
    public const string AlreadyPlayedCode = "GROUP_MATCH_ALREADY_PLAYED";

    public static Error AllMatchesPlayed(Guid groupId)
        => Error.Conflict(AllMatchesPlayedCode, $"All matches for group '{groupId}' have already been played.");

    public static Error MatchNotFound(Guid matchId)
        => Error.Conflict(MatchNotFoundCode, $"Match with ID '{matchId}' not found.");

    public static Error NegativeScore(Guid matchId)
        => Error.Conflict(NegativeScoreCode, $"Scores for match '{matchId}' cannot be negative.");

    public static Error AlreadyPlayed(Guid matchId)
        => Error.Conflict(AlreadyPlayedCode, $"Match '{matchId}' has already been played.");
}

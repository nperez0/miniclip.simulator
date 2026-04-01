using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;
using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

public class Match
{
    public Guid Id { get; private set; }
    public TeamInfo HomeTeam { get; private set; } = null!;
    public TeamInfo AwayTeam { get; private set; } = null!;
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public int Round { get; private set; }
    public bool IsPlayed { get; private set; }

    private Match()
    {
    }

    private Match(Guid id, TeamInfo homeTeam, TeamInfo awayTeam, int round)
    {
        Id = id;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        Round = round;
        IsPlayed = false;
        HomeScore = 0;
        AwayScore = 0;
    }

    public static Result<Match> Create(Guid id, TeamInfo homeTeam, TeamInfo awayTeam, int round)
        => homeTeam == awayTeam
            ? Result.Failure<Match>(GroupGenerateFixturesErrors.SameTeam(homeTeam.Id))
            : new Match(id, homeTeam, awayTeam, round);

    internal static Match Restore(Guid id, TeamInfo homeTeam, TeamInfo awayTeam, int round)
        => new(id, homeTeam, awayTeam, round);

    public Result SimulateResult(int homeScore, int awayScore)
    {
        if (homeScore < 0 || awayScore < 0)
            return Result.Failure(GroupSimulationErrors.NegativeScore(Id));

        if (IsPlayed)
            return Result.Failure(GroupSimulationErrors.AlreadyPlayed(Id));

        HomeScore = homeScore;
        AwayScore = awayScore;
        IsPlayed = true;

        return Result.Success();
    }

    internal void ApplyResult(int homeScore, int awayScore)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
        IsPlayed = true;
    }
}

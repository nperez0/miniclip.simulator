using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
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
    {
        if (homeTeam == awayTeam)
            return Result.Failure<Match>(GroupGenerateFixturesException.SameTeam());

        return new Match(id, homeTeam, awayTeam, round);
    }

    internal static Match Restore(Guid id, TeamInfo homeTeam, TeamInfo awayTeam, int round)
        => new(id, homeTeam, awayTeam, round);

    public Result SimulateResult(int homeScore, int awayScore)
    {
        if (homeScore < 0 || awayScore < 0)
            return Result.Failure(GroupSimulationException.NegativeScore());

        if (IsPlayed)
            return Result.Failure(GroupSimulationException.AlreadyPlayed(Id));

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

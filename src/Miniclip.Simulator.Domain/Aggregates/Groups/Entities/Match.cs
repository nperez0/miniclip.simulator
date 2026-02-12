using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

public class Match
{
    public Guid Id { get; }
    public Team HomeTeam { get; }
    public Team AwayTeam { get; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public int Round { get; }
    public bool IsPlayed { get; private set; }

    private Match(Guid id, Team homeTeam, Team awayTeam, int round)
    {
        Id = id;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        Round = round;
        IsPlayed = false;
        HomeScore = 0;
        AwayScore = 0;
    }

    public static Result<Match> Create(Guid id, Team homeTeam, Team awayTeam, int round)
    {
        if (homeTeam == awayTeam)
            return Result.Failure<Match>(MatchCreationException.SameTeam());

        return new Match(id, homeTeam, awayTeam, round);
    }

    public Result SimulateResult(int homeScore, int awayScore)
    {
        if (homeScore < 0 || awayScore < 0)
            return Result.Failure(MatchSimulateResultException.NegativeScore());

        if (IsPlayed)
            return Result.Failure(MatchSimulateResultException.AlreadyPlayed(Id));

        HomeScore = homeScore;
        AwayScore = awayScore;
        IsPlayed = true;

        return Result.Success();
    }
}

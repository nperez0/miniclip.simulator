using Miniclip.Core;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

public class Match
{
    public Guid Id { get; private set; }
    public Guid HomeTeamId { get; private set; }
    public Guid AwayTeamId { get; private set; }
    public Team HomeTeam { get; private set; } = null!;
    public Team AwayTeam { get; private set; } = null!;
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public int Round { get; private set; }
    public bool IsPlayed { get; private set; }

    private Match()
    {
    }

    private Match(Guid id, Team homeTeam, Team awayTeam, int round)
    {
        Id = id;
        HomeTeam = homeTeam;
        HomeTeamId = homeTeam.Id;
        AwayTeam = awayTeam;
        AwayTeamId = awayTeam.Id;
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

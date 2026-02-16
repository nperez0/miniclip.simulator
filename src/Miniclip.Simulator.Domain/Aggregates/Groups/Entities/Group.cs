using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

public class Group : AggregateRoot
{
    private readonly List<Team> teams;
    private readonly List<Match> matches;

    public const int MinCapacity = 2;
    public const int MaxCapacity = 6;

    public string Name { get; }
    public int Capacity { get; }

    public virtual IReadOnlyCollection<Team> Teams => teams.AsReadOnly();
    public virtual IReadOnlyCollection<Match> Matches => matches.AsReadOnly();

    private Group(Guid id, string name, int capacity)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
        teams = [];
        matches = [];
    }

    public static Result<Group> Create(Guid id, string? name, int capacity)
    {
        if (name.IsNullOrWhiteSpace())
            return Result.Failure<Group>(GroupCreationException.EmptyName(name));

        if (capacity < MinCapacity || capacity > MaxCapacity)
            return Result.Failure<Group>(GroupCreationException.InvalidCapacity(capacity, MinCapacity, MaxCapacity));

        return new Group(id, name, capacity);
    }

    public Result AddTeam(Team team)
    {
        if (teams.Count >= Capacity)
            return Result.Failure(GroupAddTeamException.MaxTeamsReached(Capacity));

        if (teams.Any(x => x.Id == team.Id))
            return Result.Failure(GroupAddTeamException.TeamAlreadyExists(team.Id));

        teams.Add(team);

        return Result.Success();
    }

    public Result AddMatch(Guid id, Team homeTeam, Team awayTeam, int round)
    {
        var matchResult = Match.Create(id, homeTeam, awayTeam, round);

        if (matchResult.IsFailure)
            return matchResult;
            
        matches.Add(matchResult.Value!);

        return Result.Success();
    }

    public Result SimulateMatch(Guid matchId, int homeScore, int awayScore)
    {
        var match = matches.FirstOrDefault(m => m.Id == matchId);
        
        if (match == null)
            return Result.Failure(MatchNotFoundException.NotFound(matchId));

        var result = match.SimulateResult(homeScore, awayScore);

        if (result.IsFailure)
            return result;

        Enqueue(new MatchPlayed(
            GroupId: Id,
            GroupName: Name,
            MatchId: match.Id,
            HomeTeamId: match.HomeTeam.Id,
            HomeTeamName: match.HomeTeam.Name,
            HomeTeamStrength: match.HomeTeam.Strength,
            HomeScore: match.HomeScore,
            AwayTeamId: match.AwayTeam.Id,
            AwayTeamName: match.AwayTeam.Name,
            AwayTeamStrength: match.AwayTeam.Strength,
            AwayScore: match.AwayScore,
            Round: match.Round
        ));

        return Result.Success();
    }
}

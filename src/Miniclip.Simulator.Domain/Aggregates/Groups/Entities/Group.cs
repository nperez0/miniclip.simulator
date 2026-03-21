using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Groups.ValueObjects;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

public class Group : AggregateRoot
{
    private readonly List<TeamInfo> teams = [];
    private readonly List<Match> matches = [];

    private const int MinCapacity = 2;
    private const int MaxCapacity = 6;

    public string Name { get; private set; } = null!;
    public int Capacity { get; private set; }

    public virtual IReadOnlyCollection<TeamInfo> Teams => teams.AsReadOnly();
    public virtual IReadOnlyCollection<Match> Matches => matches.AsReadOnly();

    private Group()
    {
    }

    private Group(Guid id, string name, int capacity)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
    }

    public static Result<Group> Create(Guid id, string? name, int capacity)
    {
        if (name.IsNullOrWhiteSpace())
            return Result.Failure<Group>(GroupCreationException.EmptyName(name));

        if (capacity < MinCapacity || capacity > MaxCapacity)
            return Result.Failure<Group>(GroupCreationException.InvalidCapacity(capacity, MinCapacity, MaxCapacity));

        var group = new Group(id, name, capacity);
        group.Enqueue(new GroupCreated(id, name, capacity));

        return group;
    }

    public Result AddTeam(TeamInfo teamInfo)
    {
        if (teams.Count >= Capacity)
            return Result.Failure(GroupAddTeamException.MaxTeamsReached(Capacity));

        if (teams.Any(x => x.Id == teamInfo.Id))
            return Result.Failure(GroupAddTeamException.TeamAlreadyExists(teamInfo.Id));

        teams.Add(teamInfo);
        Enqueue(new TeamAdded(Id, teamInfo.Id, teamInfo.Name, teamInfo.Strength));

        return Result.Success();
    }

    public Result AddMatch(Guid id, TeamInfo homeTeam, TeamInfo awayTeam, int round)
        => Match.Create(id, homeTeam, awayTeam, round)
            .Tap(match =>
            {
                matches.Add(match);
                Enqueue(new MatchScheduled(Id, id, homeTeam.Id, awayTeam.Id, round));
            });

    public Result SimulateMatch(Guid matchId, int homeScore, int awayScore)
    {
        var match = matches.FirstOrDefault(m => m.Id == matchId);

        if (match == null)
            return Result.Failure(GroupSimulationException.MatchNotFound(matchId));

        return match.SimulateResult(homeScore, awayScore)
            .Tap(() =>
            {
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
            });
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case GroupCreated e: Apply(e); break;
            case TeamAdded e: Apply(e); break;
            case MatchScheduled e: Apply(e); break;
            case MatchPlayed e: Apply(e); break;
        }
    }

    private void Apply(GroupCreated e)
    {
        Id = e.GroupId;
        Name = e.Name;
        Capacity = e.Capacity;
    }

    private void Apply(TeamAdded e)
        => teams.Add(new TeamInfo(e.TeamId, e.Name, e.Strength));

    private void Apply(MatchScheduled e)
    {
        var homeTeam = teams.First(t => t.Id == e.HomeTeamId);
        var awayTeam = teams.First(t => t.Id == e.AwayTeamId);
        matches.Add(Match.Restore(e.MatchId, homeTeam, awayTeam, e.Round));
    }

    private void Apply(MatchPlayed e)
        => matches.First(m => m.Id == e.MatchId).ApplyResult(e.HomeScore, e.AwayScore);
}

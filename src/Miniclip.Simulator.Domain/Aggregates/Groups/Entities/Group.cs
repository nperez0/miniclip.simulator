using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Errors;
using Miniclip.Simulator.Domain.Aggregates.Groups.Events;
using Miniclip.Simulator.Domain.Aggregates.Groups.Validations;
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
        => Validation.For<Group>()
            .HasValidName(name)
            .HasValidCapacity(capacity, MinCapacity, MaxCapacity)
            .Validate()
            .Map(() =>
            {
                var group = new Group(id, name!, capacity);
                group.Enqueue(new GroupCreated(id, name!, capacity));
                return group;
            });

    public Result AddTeam(TeamInfo teamInfo)
    {
        if (teams.Count >= Capacity)
            return Result.Failure(GroupAddTeamErrors.MaxTeamsReached(Capacity));

        if (teams.Any(x => x.Id == teamInfo.Id))
            return Result.Failure(GroupAddTeamErrors.TeamAlreadyExists(teamInfo.Id));

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
            return Result.Failure(GroupSimulationErrors.MatchNotFound(matchId));

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

    private void Apply(GroupCreated @event)
    {
        Id = @event.GroupId;
        Name = @event.Name;
        Capacity = @event.Capacity;
    }

    private void Apply(TeamAdded @event)
        => teams.Add(new TeamInfo(@event.TeamId, @event.Name, @event.Strength));

    private void Apply(MatchScheduled @event)
    {
        var homeTeam = teams.First(t => t.Id == @event.HomeTeamId);
        var awayTeam = teams.First(t => t.Id == @event.AwayTeamId);
        matches.Add(Match.Restore(@event.MatchId, homeTeam, awayTeam, @event.Round));
    }

    private void Apply(MatchPlayed @event)
        => matches.First(m => m.Id == @event.MatchId).ApplyResult(@event.HomeScore, @event.AwayScore);
}

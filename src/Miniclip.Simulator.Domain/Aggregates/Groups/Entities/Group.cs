using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;
using System.Text.RegularExpressions;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

public class Group : AggregateRoot
{
    private readonly List<Team> teams;
    private readonly List<Match> matches;

    public string Name { get; }
    public int Capacity { get; }

    public IReadOnlyCollection<Team> Teams => teams.AsReadOnly();
    public IReadOnlyCollection<Match> Matches => matches.AsReadOnly();

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

        if (capacity < 2)
            return Result.Failure<Group>(GroupCreationException.MinimumCapacity());

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

    public void AddMatch(Guid id, Team homeTeam, Team awayTeam, int round)
    {
        var matchResult = Match.Create(id, homeTeam, awayTeam, round);

        if (matchResult.IsSuccess)
            matches.Add(matchResult.Value!);
    }
}

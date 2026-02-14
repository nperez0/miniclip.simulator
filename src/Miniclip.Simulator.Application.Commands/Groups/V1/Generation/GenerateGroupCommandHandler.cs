using MediatR;
using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Fixtures;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Generation;

public class GenerateGroupCommandHandler(
    IRepository<Group> groupsRepository,
    IRepository<Team> teamsRepository,
    IFixtureSchedulerService fixtureSchedulerService)
    : IRequestHandler<GenerateGroupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GenerateGroupCommand command, CancellationToken cancellationToken)
    {
        var groupResult = Group.Create(Guid.NewGuid(), command.Name, command.Capacity);

        if (groupResult.IsFailure)
            return Result.Failure<Guid>(groupResult.Exception);

        var group = groupResult.Value!;
        var teams = await GetRandomTeams(command.Capacity, cancellationToken);

        foreach (var team in teams)
        {
            var addTeamResult = group.AddTeam(team);
            if (addTeamResult.IsFailure)
                return Result.Failure<Guid>(addTeamResult.Exception);
        }

        var schedulerResult = fixtureSchedulerService.GenerateFixtures(group);

        if (schedulerResult.IsFailure)
            return Result.Failure<Guid>(schedulerResult.Exception);

        groupsRepository.Add(group);

        return Result.Success(groupResult.Value!.Id);
    }

    public async Task<IEnumerable<Team>> GetRandomTeams(int count, CancellationToken cancellationToken)
    {
        var allTeams = await teamsRepository.GetAllAsync(cancellationToken);

        return allTeams
            .OrderBy(_ => Guid.NewGuid())
            .Take(count);
    }
}

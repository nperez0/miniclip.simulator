using Mediator;
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
    public async ValueTask<Result<Guid>> Handle(GenerateGroupCommand command, CancellationToken cancellationToken)
    {
        var teams = await GetRandomTeams(command.Capacity, cancellationToken);

        return Group.Create(Guid.NewGuid(), command.Name, command.Capacity)
            .Then(group => AddTeams(group, teams))
            .Then(fixtureSchedulerService.GenerateFixtures)
            .Tap(groupsRepository.Add)
            .Map(group => group.Id);
    }

    private static Result<Group> AddTeams(Group group, IEnumerable<Team> teams)
        => teams.Traverse(group.AddTeam)
            .Map(() => group);

    public async Task<IEnumerable<Team>> GetRandomTeams(int count, CancellationToken cancellationToken)
    {
        var allTeams = await teamsRepository.GetAllAsync(cancellationToken);

        return allTeams
            .OrderBy(_ => Guid.NewGuid())
            .Take(count);
    }
}

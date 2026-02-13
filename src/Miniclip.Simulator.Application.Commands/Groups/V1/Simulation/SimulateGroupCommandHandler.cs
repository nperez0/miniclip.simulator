using MediatR;
using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Groups.Services.Simulator;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public class SimulateGroupCommandHandler(
    IRepository<Group> repository,
    IGroupSimulator groupSimulator) 
    : IRequestHandler<SimulateGroupCommand, Result>
{
    public async Task<Result> Handle(SimulateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await repository.FindAsync(command.GroupId, cancellationToken);
        
        if (group == null)
            return Result.Failure(new Exception($"Group with id {command.GroupId} not found"));

        groupSimulator.SimulateAllMatches(group);

        // TODO: Publish domain event for read model projection
        // This will trigger the GroupSimulatedProjection to update read models

        return Result.Success();
    }
}

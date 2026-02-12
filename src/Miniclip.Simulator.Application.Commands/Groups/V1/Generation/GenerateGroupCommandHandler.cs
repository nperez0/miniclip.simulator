using MediatR;
using Miniclip.Core;
using Miniclip.Core.Domain;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Generation;

public class GenerateGroupCommandHandler(IRepository<Group> repository) : IRequestHandler<GenerateGroupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GenerateGroupCommand command, CancellationToken cancellationToken)
    {
        var groupResult = Group.Create(Guid.NewGuid(), command.Name, command.Capacity);

        if (groupResult.IsFailure)
            return Result.Failure<Guid>(groupResult.Exception);

        repository.Add(groupResult.Value!);

        return Result.Success(groupResult.Value!.Id);
    }
}

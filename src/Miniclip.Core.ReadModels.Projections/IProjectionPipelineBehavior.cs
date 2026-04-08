using Miniclip.Core.Domain;

namespace Miniclip.Core.ReadModels.Projections;

public delegate ValueTask ProjectionHandlerDelegate(CancellationToken ct);

public interface IProjectionPipelineBehavior
{
    ValueTask HandleAsync(IDomainEvent @event, CancellationToken ct, ProjectionHandlerDelegate next);
}

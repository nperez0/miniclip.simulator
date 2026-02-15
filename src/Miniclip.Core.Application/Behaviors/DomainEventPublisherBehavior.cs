using MediatR;
using Miniclip.Core.Application.Extensions;
using Miniclip.Core.Domain;

namespace Miniclip.Core.Application.Behaviors;

public class DomainEventPublisherBehavior<TRequest, TResponse>(IPublisher publisher, IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (!request.IsCommand() || !response.IsSuccessful())
            return response;

        // Get domain events from aggregates
        var aggregates = unitOfWork.GetTrackedAggregates();
        var events = aggregates.SelectMany(a => a.DequeueUncommittedEvents());

        // Publish via MediatR
        foreach (var @event in events)
            await publisher.Publish(@event, cancellationToken);

        return response;
    }
}

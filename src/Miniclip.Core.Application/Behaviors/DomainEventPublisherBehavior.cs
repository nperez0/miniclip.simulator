using Mediator;
using Miniclip.Core.Application.Extensions;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Application.Behaviors;

public class DomainEventPublisherBehavior<TRequest, TResponse>(IEventBus eventBus, IEventStoreSession session)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(request, cancellationToken);

        if (!request.IsCommand() || !response.IsSuccessful())
            return response;

        foreach (var @event in session.GetCommittedEvents())
            await eventBus.PublishAsync(@event, cancellationToken);

        return response;
    }
}

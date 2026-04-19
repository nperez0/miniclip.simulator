using Mediator;
using Miniclip.Core.Application.Extensions;
using Miniclip.Core.Application.Publishers;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Application.Behaviors;

public class EventStoreCommandBehavior<TRequest, TResponse>(
    ICommittedEventPublisher publisher,
    IEventStoreSession session)
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

        await session.CommitAsync(cancellationToken);

        foreach (var committed in session.GetCommittedEvents())
            await publisher.PublishAsync(committed, cancellationToken);

        return response;
    }
}
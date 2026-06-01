using Miniclip.Core.Application.Extensions;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Application.Behaviors;

public class EventStoreCommandBehavior<TRequest, TResponse>(
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

        return response;
    }
}
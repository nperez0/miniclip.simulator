using Mediator;
using Miniclip.Core.Application.Extensions;
using Miniclip.Core.Domain;

namespace Miniclip.Core.Application.Behaviors;

public class CommandUnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!request.IsCommand())
            return await next(request, cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(request, cancellationToken);

            if (response.IsSuccessful())
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
            }
            else
                await unitOfWork.RollbackAsync(cancellationToken);

            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

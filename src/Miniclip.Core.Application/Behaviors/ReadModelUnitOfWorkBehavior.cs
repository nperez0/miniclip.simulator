using MediatR;
using Miniclip.Core.Application.Extensions;
using Miniclip.Core.ReadModels;

namespace Miniclip.Core.Application.Behaviors;

public class ReadModelUnitOfWorkBehavior<TRequest, TResponse>(IReadModelUnitOfWork unitOfWork)
: IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!request.IsCommand())
            return await next(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(cancellationToken);

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

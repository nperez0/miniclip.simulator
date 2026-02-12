using MediatR;
using Miniclip.Core.Domain;

namespace Miniclip.Core.Application.Behaviors;

public class CommandUnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand(request))
            return await next(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(cancellationToken);

            if (IsSuccessful(response))
                await unitOfWork.CommitAsync(cancellationToken);
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

    private static bool IsCommand(TRequest request)
    {
        if (request is ICommand)
            return true;

        var requestType = request.GetType();
        var interfaces = requestType.GetInterfaces();
        
        return interfaces.Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }

    private static bool IsSuccessful(TResponse response)
    {
        return response switch
        {
            Result result => result.IsSuccess,
            _ => true
        };
    }
}

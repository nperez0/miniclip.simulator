using MediatR;

namespace Miniclip.Core.Application;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResult> : IRequest<Result<TResult>>
{
}

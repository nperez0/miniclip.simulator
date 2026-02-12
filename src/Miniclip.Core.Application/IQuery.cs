using MediatR;

namespace Miniclip.Core.Application;

public interface IQuery<TResult> : IRequest<TResult>
{
}

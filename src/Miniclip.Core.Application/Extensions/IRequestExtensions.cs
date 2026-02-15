using MediatR;

namespace Miniclip.Core.Application.Extensions;

internal static class IRequestExtensions
{
    internal static bool IsCommand<TRequest>(this TRequest request)
        where TRequest : IBaseRequest
    {
        if (request is ICommand)
            return true;

        var requestType = request.GetType();
        var interfaces = requestType.GetInterfaces();

        return interfaces.Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }
}

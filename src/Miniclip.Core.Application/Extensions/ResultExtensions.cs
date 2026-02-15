namespace Miniclip.Core.Application.Extensions;

public static class ResultExtensions
{
    public static bool IsSuccessful<TResponse>(this TResponse response)
    {
        return response switch
        {
            Result result => result.IsSuccess,
            _ => true
        };
    }
}

using Miniclip.Core;

namespace Miniclip.Simulator.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
        => result.IsSuccess
            ? new NoContentResult()
            : ProcessFailedResult(result.Error);

    public static IActionResult ToActionResult<T>(this Result<T> result)
        => result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProcessFailedResult(result.Error);

    private static IActionResult ProcessFailedResult(Error error)
        => error.Type switch
        {
            ErrorType.Validation 
                => new BadRequestObjectResult(new { errors = error.Messages }),

            ErrorType.NotFound
                => new NotFoundObjectResult(new { error = error.Messages[0] }),

            _ => new BadRequestObjectResult(new { error = error.Messages[0] })
        };
}

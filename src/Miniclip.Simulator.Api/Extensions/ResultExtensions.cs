using Microsoft.AspNetCore.Mvc;
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
    {
        return error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(new { error = error.Message }),
            _ => new BadRequestObjectResult(new { error = error.Message })
        };
    }
}

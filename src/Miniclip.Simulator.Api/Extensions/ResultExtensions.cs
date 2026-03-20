using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;

namespace Miniclip.Simulator.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
        => result.IsSuccess
            ? new NoContentResult()
            : ProcessFailedResult(result.Exception);

    public static IActionResult ToActionResult<T>(this Result<T> result)
        => result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProcessFailedResult(result.Exception);

    private static IActionResult ProcessFailedResult(ExceptionBase ex)
    {
        return ex.Type switch
        {
            ExceptionType.NotFound => new NotFoundObjectResult(new { error = ex.Message }),
            _ => new BadRequestObjectResult(new { error = ex.Message })
        };
    }
}

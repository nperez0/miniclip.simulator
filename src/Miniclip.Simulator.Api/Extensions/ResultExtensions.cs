using Microsoft.AspNetCore.Mvc;
using Miniclip.Core;
using Miniclip.Core.Domain.Exceptions;

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

    private static IActionResult ProcessFailedResult(Exception ex)
    {
        return ex switch
        {
            NotFoundException => new NotFoundObjectResult(new { error = ex.Message }),
            _ => new BadRequestObjectResult(new { error = ex.Message })
        };
    }
}

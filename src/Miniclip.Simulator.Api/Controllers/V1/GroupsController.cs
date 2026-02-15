using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Simulator.Api.Extensions;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

namespace Miniclip.Simulator.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class GroupsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup(
        [FromBody] GenerateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request.ToCommand(), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Simulate all matches in a group
    /// </summary>
    [HttpPost("{groupId}/simulate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SimulateGroup(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var command = new SimulateGroupCommand(groupId);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Get group standings (team rankings)
    /// </summary>
    [HttpGet("{id}/standings")]
    [ProducesResponseType(typeof(IEnumerable<GroupStandingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStandings(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GroupStandingsQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}

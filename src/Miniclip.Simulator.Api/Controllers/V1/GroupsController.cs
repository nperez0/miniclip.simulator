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
    /// <summary>
    /// Generates a new group with random teams from the database.
    /// </summary>
    /// <param name="request">The group details including capacity (2-6 teams).</param>
    /// <returns>The unique identifier of the created group (204) or validation error (400).</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup(
        [FromBody] GenerateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request.ToCommand(), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Simulates all matches in a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group to simulate.</param>
    /// <returns>A result indicating the outcome of the simulation.</returns>
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
    /// Gets the standings (rankings) for a specific group.
    /// </summary>
    /// <param name="id">The unique identifier of the group.</param>
    /// <returns>A list of team standings for the specified group.</returns>
    [HttpGet("{id}/standings")]
    [ProducesResponseType(typeof(IEnumerable<GroupStandingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStandings(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GroupStandingsQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}

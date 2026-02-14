using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Miniclip.Simulator.Application.Commands.Groups.V1.Generation;
using Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;
using Miniclip.Simulator.Application.Queries.Groups.V1.MatchResults;
using Miniclip.Simulator.Application.Queries.Groups.V1.Overview;
using Miniclip.Simulator.Application.Queries.Groups.V1.Standings;

namespace Miniclip.Simulator.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class GroupsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Create a new group and generate fixtures
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup(
        [FromBody] GenerateGroupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction("GetOverview", new { id = result.Value }, result.Value);

        return BadRequest(result.Exception.Message);
    }

    /// <summary>
    /// Simulate all matches in a group
    /// </summary>
    [HttpPost("{id}/simulate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SimulateGroup(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new SimulateGroupCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        return result.Exception.Message.Contains("not found") ? NotFound(result.Exception.Message) : BadRequest(result.Exception.Message);
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

    /// <summary>
    /// Get all match results in a group
    /// </summary>
    [HttpGet("{id}/matches")]
    [ProducesResponseType(typeof(IEnumerable<MatchResultsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatches(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new MatchResultsQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get complete group overview (standings + matches)
    /// </summary>
    //[HttpGet("{id}/overview")]
    //[ProducesResponseType(typeof(GroupOverviewDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> GetOverview(
    //    Guid id,
    //    CancellationToken cancellationToken)
    //{
    //    var query = new GroupOverviewQuery(id);
    //    var result = await mediator.Send(query, cancellationToken);

    //    return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    //}
}

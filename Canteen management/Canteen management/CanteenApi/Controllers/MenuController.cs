using Canteen.Application.DTOs;
using Canteen.Application.Features.Menu.Commands;
using Canteen.Application.Features.Menu.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanteenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly ISender _mediator;

    public MenuController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyMenu([FromQuery] DateOnly? startDate)
    {
        var result = await _mediator.Send(new GetWeeklyMenuScheduleQuery(startDate));
        return Ok(result);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayMenu([FromQuery] DateOnly? date)
    {
        var result = await _mediator.Send(new GetDailyMenuQuery(date));
        if (result == null) return NotFound("Menu for today is not available.");
        return Ok(result);
    }

    [Authorize(Roles = "CanteenManager")]
    [HttpPost("items")]
    public async Task<IActionResult> CreateMenuItem([FromBody] CreateMenuItemCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWeeklyMenu), new { id = result.Id }, result);
    }

    [Authorize(Roles = "CanteenManager")]
    [HttpPut("schedules/{id:guid}/capacity")]
    public async Task<IActionResult> UpdateCapacity(Guid id, [FromBody] UpdateScheduleCapacityRequest request)
    {
        var command = new UpdateScheduleCapacityCommand(id, request.NewCapacity, request.NewPrice, request.NewCutoffTime);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

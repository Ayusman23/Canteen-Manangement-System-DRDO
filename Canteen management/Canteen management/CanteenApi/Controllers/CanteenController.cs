using Canteen.Application.Features.Bookings.Commands;
using Canteen.Application.Features.Bookings.Queries;
using Canteen.Application.Features.Menu.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CanteenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CanteenController : ControllerBase
{
    private readonly ISender _mediator;

    public CanteenController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu()
    {
        var menu = await _mediator.Send(new GetLegacyWeeklyMenuQuery());
        return Ok(menu);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayMenu()
    {
        var todayMenu = await _mediator.Send(new GetLegacyTodayMenuQuery());
        if (todayMenu == null)
            return NotFound("Today's menu not found.");

        return Ok(todayMenu);
    }

    [HttpPost("book")]
    public async Task<IActionResult> BookMeal([FromBody] BookingRequest request)
    {
        if (request == null) return BadRequest("Invalid request.");

        var command = new CreateBookingCommand(
            Name: request.Name,
            MealType: request.MealType ?? "Normal",
            Day: request.Day,
            Date: null,
            ScheduleId: null,
            UserId: null,
            Email: null,
            EmployeeCode: null
        );

        var result = await _mediator.Send(command);

        return Ok(new
        {
            Token = result.Token,
            Message = result.Message,
            Details = new
            {
                Id = result.Details.Id,
                Name = result.Details.UserName,
                MealType = result.Details.MealType.ToString(),
                Day = request.Day ?? result.Details.ScheduledMealDate.DayOfWeek.ToString(),
                Token = result.Token,
                BookingDate = result.Details.BookingDate
            }
        });
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings()
    {
        var bookings = await _mediator.Send(new GetAllBookingsQuery());
        return Ok(bookings.Select(b => new
        {
            Id = b.Id,
            Name = b.UserName,
            MealType = b.MealType.ToString(),
            Day = b.ScheduledMealDate.DayOfWeek.ToString(),
            Token = b.BookingReference,
            BookingDate = b.BookingDate
        }));
    }
}

public class BookingRequest
{
    public string Name { get; set; } = string.Empty;
    public string MealType { get; set; } = "Normal";
    public string Day { get; set; } = string.Empty;
}
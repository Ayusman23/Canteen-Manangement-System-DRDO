using System.Security.Claims;
using Canteen.Application.DTOs;
using Canteen.Application.Features.Bookings.Commands;
using Canteen.Application.Features.Bookings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanteenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly CanteenApi.Services.IEmailService _emailService;

    public BookingsController(ISender mediator, CanteenApi.Services.IEmailService emailService)
    {
        _mediator = mediator;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        Guid? userId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var parsedId))
        {
            userId = parsedId;
        }

        var command = new CreateBookingCommand(
            Name: request.Name,
            MealType: request.MealType,
            Day: request.Day,
            Date: request.Date,
            ScheduleId: request.ScheduleId,
            UserId: userId,
            Email: request.Email ?? User.FindFirstValue(ClaimTypes.Email),
            EmployeeCode: request.EmployeeCode ?? User.FindFirstValue("EmployeeCode")
        );

        var result = await _mediator.Send(command);

        // Send booking confirmation email asynchronously in background
        if (result.Details != null && !string.IsNullOrWhiteSpace(result.Details.UserEmail))
        {
            _ = _emailService.SendBookingConfirmationAsync(
                recipientEmail: result.Details.UserEmail,
                recipientName: result.Details.UserName,
                tokenRef: result.Token,
                mealName: result.Details.MealName,
                mealType: result.Details.MealType.ToString(),
                mealDate: result.Details.ScheduledMealDate,
                price: result.Details.Price,
                qrHash: result.Details.QrCodeHash ?? ""
            );
        }

        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings([FromQuery] string? email)
    {
        Guid? userId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var parsedId))
        {
            userId = parsedId;
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? email;
        var result = await _mediator.Send(new GetUserBookingsQuery(userId, userEmail));
        return Ok(result);
    }

    [HttpGet("token/{token}")]
    public async Task<IActionResult> GetBookingByToken(string token)
    {
        var result = await _mediator.Send(new GetBookingByTokenQuery(token));
        return Ok(result);
    }

    [Authorize(Roles = "KitchenOperator,CanteenManager")]
    [HttpPost("dispense")]
    public async Task<IActionResult> DispenseMeal([FromBody] DispenseMealRequest request)
    {
        var operatorName = User.FindFirstValue(ClaimTypes.Name) ?? request.DispensedBy ?? "Kitchen Operator";
        var command = new DispenseMealCommand(request.Token, operatorName, request.Notes);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelBooking([FromBody] CancelBookingRequest request)
    {
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "User";
        var command = new CancelBookingCommand(request.Token, request.Reason, userName);
        var result = await _mediator.Send(command);
        return Ok(new { Success = result, Message = "Booking successfully cancelled." });
    }

    [Authorize(Roles = "CanteenManager")]
    [HttpGet]
    public async Task<IActionResult> GetAllBookings([FromQuery] DateOnly? date)
    {
        var result = await _mediator.Send(new GetAllBookingsQuery(date));
        return Ok(result);
    }
}

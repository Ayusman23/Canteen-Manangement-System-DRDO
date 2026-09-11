using Canteen.Application.Common.Interfaces;
using CanteenApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanteenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IApplicationDbContext context,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePaymentOrderRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { Message = "Payment amount must be greater than zero." });
        }

        var receipt = $"rcpt_{DateTime.UtcNow:yyyyMMddHHmmss}_{request.MealType ?? "MEAL"}";
        var order = await _paymentService.CreateOrderAsync(request.Amount, receipt);

        return Ok(order);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId) || string.IsNullOrWhiteSpace(request.PaymentId))
        {
            return BadRequest(new { Message = "OrderId and PaymentId are required." });
        }

        var isValid = _paymentService.VerifySignature(request.OrderId, request.PaymentId, request.Signature ?? "");
        if (!isValid)
        {
            return BadRequest(new { Success = false, Message = "Invalid payment signature verification." });
        }

        if (!string.IsNullOrWhiteSpace(request.BookingReference))
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingReference == request.BookingReference);
            if (booking != null)
            {
                // Note payment details in booking or audit log
                var audit = new Canteen.Domain.Entities.TokenAuditLog
                {
                    BookingId = booking.Id,
                    Action = "PaymentSuccess",
                    PerformedByUserId = booking.UserName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "online",
                    Notes = $"Paid ₹{booking.Price} via Razorpay ID: {request.PaymentId}"
                };
                _context.TokenAuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new
        {
            Success = true,
            Message = "Payment successfully verified and token activated.",
            PaymentId = request.PaymentId,
            OrderId = request.OrderId
        });
    }
}

public class CreatePaymentOrderRequest
{
    public decimal Amount { get; set; }
    public string? MealType { get; set; }
    public Guid? ScheduleId { get; set; }
    public string? BookingReference { get; set; }
}

public class VerifyPaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public string? BookingReference { get; set; }
}

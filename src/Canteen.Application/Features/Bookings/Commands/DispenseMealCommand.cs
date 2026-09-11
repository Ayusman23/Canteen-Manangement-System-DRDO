using Canteen.Application.Common.Exceptions;
using Canteen.Application.Common.Interfaces;
using Canteen.Application.DTOs;
using Canteen.Domain.Entities;
using Canteen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Application.Features.Bookings.Commands;

public record DispenseMealCommand(
    string TokenOrQr,
    string DispensedBy,
    string? Notes = null
) : IRequest<DispenseMealResponse>;

public class DispenseMealCommandHandler : IRequestHandler<DispenseMealCommand, DispenseMealResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICanteenRealTimeNotifier _notifier;

    public DispenseMealCommandHandler(IApplicationDbContext context, ICanteenRealTimeNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task<DispenseMealResponse> Handle(DispenseMealCommand request, CancellationToken cancellationToken)
    {
        var cleanInput = request.TokenOrQr.Trim();

        // Check if QR data format (e.g. "DRDO-20260911-1234|...")
        string tokenRef = cleanInput;
        if (cleanInput.Contains('|'))
        {
            var parts = cleanInput.Split('|');
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                tokenRef = parts[0];
            }
        }

        var booking = await _context.Bookings
            .Include(b => b.Schedule)
            .FirstOrDefaultAsync(b => b.BookingReference == tokenRef || b.QrCodeHash == cleanInput, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException("Booking Token", tokenRef);
        }

        if (booking.Status == BookingStatus.Dispensed)
        {
            throw new AppException($"Security Alert: Token {booking.BookingReference} was ALREADY DISPENSED at {booking.DispensedAt:HH:mm:ss} by {booking.DispensedByUserId ?? "Operator"}.", 409, "ALREADY_DISPENSED");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new AppException($"Security Alert: Token {booking.BookingReference} was CANCELLED and cannot be honored.", 400, "TOKEN_CANCELLED");
        }

        // Validate date (warn if future or past date)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (booking.ScheduledMealDate != today)
        {
            // Allowed with note or audit log
        }

        booking.Status = BookingStatus.Dispensed;
        booking.DispensedAt = DateTime.UtcNow;
        booking.DispensedByUserId = request.DispensedBy;

        var audit = new TokenAuditLog
        {
            BookingId = booking.Id,
            Action = "Dispensed",
            PerformedByUserId = request.DispensedBy,
            IpAddress = "kiosk",
            Notes = request.Notes ?? $"Meal dispensed at kitchen counter for {booking.ScheduledMealDate}"
        };
        _context.TokenAuditLogs.Add(audit);

        await _context.SaveChangesAsync(cancellationToken);

        // Real-time broadcast
        await _notifier.BroadcastTokenDispensedAsync(
            booking.BookingReference,
            booking.MealName,
            request.DispensedBy,
            booking.DispensedAt.Value,
            cancellationToken
        );

        return new DispenseMealResponse(
            Success: true,
            Message: $"Meal successfully validated and dispensed for {booking.UserName}!",
            BookingReference: booking.BookingReference,
            UserName: booking.UserName,
            MealName: booking.MealName,
            DispensedAt: booking.DispensedAt.Value
        );
    }
}

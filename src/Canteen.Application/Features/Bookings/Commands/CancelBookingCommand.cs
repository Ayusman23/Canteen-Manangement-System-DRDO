using Canteen.Application.Common.Exceptions;
using Canteen.Application.Common.Interfaces;
using Canteen.Domain.Entities;
using Canteen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Application.Features.Bookings.Commands;

public record CancelBookingCommand(
    string BookingReference,
    string Reason,
    string PerformedBy
) : IRequest<bool>;

public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICanteenRealTimeNotifier _notifier;

    public CancelBookingCommandHandler(IApplicationDbContext context, ICanteenRealTimeNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task<bool> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.Schedule)
            .FirstOrDefaultAsync(b => b.BookingReference == request.BookingReference, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException("Booking", request.BookingReference);
        }

        if (booking.Status == BookingStatus.Dispensed)
        {
            throw new AppException("Cannot cancel a meal that has already been dispensed.", 400, "CANNOT_CANCEL_DISPENSED");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return true; // Already cancelled
        }

        booking.Status = BookingStatus.Cancelled;

        // Release schedule slot
        if (booking.Schedule != null)
        {
            booking.Schedule.ReleaseSlot();
        }

        var audit = new TokenAuditLog
        {
            BookingId = booking.Id,
            Action = "Cancelled",
            PerformedByUserId = request.PerformedBy,
            IpAddress = "internal",
            Notes = $"Reason: {request.Reason}"
        };
        _context.TokenAuditLogs.Add(audit);

        await _context.SaveChangesAsync(cancellationToken);

        if (booking.Schedule != null)
        {
            await _notifier.BroadcastMealAvailabilityUpdatedAsync(
                booking.Schedule.Id,
                booking.MealName,
                booking.Schedule.CurrentBookingsCount,
                booking.Schedule.MaxCapacity,
                cancellationToken
            );
        }

        return true;
    }
}

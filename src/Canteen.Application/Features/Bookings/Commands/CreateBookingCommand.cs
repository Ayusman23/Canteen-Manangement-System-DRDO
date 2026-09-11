using System.Security.Cryptography;
using System.Text;
using Canteen.Application.Common.Exceptions;
using Canteen.Application.Common.Interfaces;
using Canteen.Application.DTOs;
using Canteen.Domain.Entities;
using Canteen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Application.Features.Bookings.Commands;

public record CreateBookingCommand(
    string Name,
    string MealType,
    string? Day,
    DateOnly? Date,
    Guid? ScheduleId,
    Guid? UserId,
    string? Email,
    string? EmployeeCode
) : IRequest<CreateBookingResponse>;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICanteenRealTimeNotifier _notifier;

    public CreateBookingCommandHandler(IApplicationDbContext context, ICanteenRealTimeNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // Parse requested meal type
        if (!Enum.TryParse<MealType>(request.MealType, true, out var mealTypeEnum))
        {
            mealTypeEnum = MealType.Normal;
        }

        // Determine target date
        DateOnly targetDate;
        if (request.Date.HasValue)
        {
            targetDate = request.Date.Value;
        }
        else if (!string.IsNullOrEmpty(request.Day))
        {
            targetDate = ResolveNextDateForDayOfWeek(request.Day);
        }
        else
        {
            targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        // Find schedule with pessimistic or tracking lock
        DailyMenuSchedule? schedule = null;
        if (request.ScheduleId.HasValue && request.ScheduleId.Value != Guid.Empty)
        {
            schedule = await _context.DailyMenuSchedules
                .Include(s => s.MenuItem)
                .FirstOrDefaultAsync(s => s.Id == request.ScheduleId.Value, cancellationToken);
        }
        else
        {
            schedule = await _context.DailyMenuSchedules
                .Include(s => s.MenuItem)
                .FirstOrDefaultAsync(s => s.Date == targetDate && s.MealType == mealTypeEnum && s.IsActive, cancellationToken);
        }

        if (schedule == null)
        {
            // Fallback: If no specific schedule was created for this future date, check if a general weekday template exists
            var dayOfWeek = targetDate.DayOfWeek;
            schedule = await _context.DailyMenuSchedules
                .Include(s => s.MenuItem)
                .FirstOrDefaultAsync(s => s.DayOfWeek == dayOfWeek && s.MealType == mealTypeEnum && s.IsActive, cancellationToken);
        }

        if (schedule == null)
        {
            throw new NotFoundException("DailyMenuSchedule", $"{targetDate} ({mealTypeEnum})");
        }

        // Concurrency / Capacity Check
        if (!schedule.HasCapacity())
        {
            throw new CapacityExceededException($"The maximum booking capacity ({schedule.MaxCapacity}) for {schedule.MealType} on {schedule.Date} has been reached.");
        }

        // Cutoff time check for same-day bookings
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (schedule.Date == today)
        {
            var nowTime = DateTime.UtcNow.TimeOfDay;
            if (nowTime > schedule.CutoffTime)
            {
                throw new AppException($"The daily reservation cut-off time ({schedule.CutoffTime:hh\\:mm}) for today has passed.", 400, "CUTOFF_EXCEEDED");
            }
        }

        // Increment capacity atomically
        schedule.ReserveSlot();

        // Generate unique DRDO token: DRDO-YYYYMMDD-XXXX
        var randomSuffix = RandomNumberGenerator.GetInt32(1000, 9999);
        var tokenRef = $"DRDO-{targetDate:yyyyMMdd}-{randomSuffix}";

        // Compute security QR code payload & hash
        var qrData = $"{tokenRef}|{request.Name}|{schedule.MealType}|{targetDate:yyyy-MM-dd}|{schedule.Price}";
        var qrHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(qrData)));

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = tokenRef,
            UserId = request.UserId,
            UserName = request.Name,
            UserEmail = request.Email ?? $"{request.Name.ToLower().Replace(" ", ".")}@drdo.gov.in",
            EmployeeCode = request.EmployeeCode ?? $"DRDO-{RandomNumberGenerator.GetInt32(100, 999)}",
            ScheduleId = schedule.Id,
            MealType = schedule.MealType,
            MealName = schedule.MenuItem?.Name ?? $"{schedule.MealType} Thali",
            Price = schedule.Price,
            ScheduledMealDate = schedule.Date,
            Status = BookingStatus.Confirmed,
            BookingDate = DateTime.UtcNow,
            QrCodeHash = qrHash
        };

        var auditLog = new TokenAuditLog
        {
            BookingId = booking.Id,
            Action = "Created",
            PerformedByUserId = request.UserId?.ToString() ?? request.Name,
            IpAddress = "internal",
            Notes = $"Pre-booked for {schedule.Date:yyyy-MM-dd} {schedule.MealType}"
        };

        _context.Bookings.Add(booking);
        _context.TokenAuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("A concurrent reservation conflict occurred while securing your meal slot. Please retry.");
        }

        // Broadcast real-time inventory update
        await _notifier.BroadcastMealAvailabilityUpdatedAsync(
            schedule.Id,
            booking.MealName,
            schedule.CurrentBookingsCount,
            schedule.MaxCapacity,
            cancellationToken
        );

        var detailsDto = new BookingDto(
            booking.Id,
            booking.BookingReference,
            booking.UserId,
            booking.UserName,
            booking.UserEmail,
            booking.EmployeeCode,
            booking.ScheduleId,
            booking.MealType,
            booking.MealName,
            booking.Price,
            booking.ScheduledMealDate,
            booking.Status,
            booking.BookingDate,
            booking.DispensedAt,
            booking.DispensedByUserId,
            booking.QrCodeHash
        );

        return new CreateBookingResponse(
            Token: tokenRef,
            Message: $"Meal successfully reserved for {targetDate:dddd, MMM dd}!",
            Details: detailsDto,
            QrCodeData: qrData
        );
    }

    private static DateOnly ResolveNextDateForDayOfWeek(string dayName)
    {
        if (Enum.TryParse<DayOfWeek>(dayName, true, out var targetDayOfWeek))
        {
            var today = DateTime.UtcNow;
            int daysToAdd = ((int)targetDayOfWeek - (int)today.DayOfWeek + 7) % 7;
            return DateOnly.FromDateTime(today.AddDays(daysToAdd));
        }
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

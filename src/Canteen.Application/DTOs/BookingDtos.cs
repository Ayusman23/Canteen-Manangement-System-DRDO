using Canteen.Domain.Enums;

namespace Canteen.Application.DTOs;

public record BookingDto(
    Guid Id,
    string BookingReference,
    Guid? UserId,
    string UserName,
    string UserEmail,
    string EmployeeCode,
    Guid ScheduleId,
    MealType MealType,
    string MealName,
    decimal Price,
    DateOnly ScheduledMealDate,
    BookingStatus Status,
    DateTime BookingDate,
    DateTime? DispensedAt,
    string? DispensedByUserId,
    string QrCodeHash
);

public record CreateBookingRequest(
    string Name,
    string MealType,
    string? Day = null,
    DateOnly? Date = null,
    Guid? ScheduleId = null,
    string? Email = null,
    string? EmployeeCode = null
);

public record CreateBookingResponse(
    string Token,
    string Message,
    BookingDto Details,
    string QrCodeData
);

public record DispenseMealRequest(
    string Token,
    string? DispensedBy = null,
    string? Notes = null
);

public record DispenseMealResponse(
    bool Success,
    string Message,
    string BookingReference,
    string UserName,
    string MealName,
    DateTime DispensedAt
);

public record CancelBookingRequest(
    string Token,
    string Reason
);

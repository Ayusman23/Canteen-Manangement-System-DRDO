using System.ComponentModel.DataAnnotations;
using Canteen.Domain.Enums;

namespace Canteen.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookingReference { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;

    public Guid ScheduleId { get; set; }
    public DailyMenuSchedule? Schedule { get; set; }

    public MealType MealType { get; set; }
    public string MealName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateOnly ScheduledMealDate { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public DateTime? DispensedAt { get; set; }
    public string? DispensedByUserId { get; set; }

    public string QrCodeHash { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<TokenAuditLog> AuditLogs { get; set; } = new List<TokenAuditLog>();
}

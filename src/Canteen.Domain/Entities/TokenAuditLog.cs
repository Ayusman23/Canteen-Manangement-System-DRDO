namespace Canteen.Domain.Entities;

public class TokenAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public string Action { get; set; } = string.Empty; // "Created", "Validated", "Dispensed", "Cancelled"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

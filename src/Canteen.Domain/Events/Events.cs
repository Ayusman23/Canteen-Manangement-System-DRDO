using Canteen.Domain.Entities;
using Canteen.Domain.Enums;

namespace Canteen.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public record BookingCreatedEvent(Booking Booking) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record BookingCancelledEvent(Guid BookingId, Guid ScheduleId, string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MealDispensedEvent(Guid BookingId, string Reference, string DispensedBy) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ScheduleCapacityUpdatedEvent(Guid ScheduleId, int NewCapacity, int CurrentCount) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

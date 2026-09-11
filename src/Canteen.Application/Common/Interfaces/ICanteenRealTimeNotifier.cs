namespace Canteen.Application.Common.Interfaces;

public interface ICanteenRealTimeNotifier
{
    Task BroadcastMealAvailabilityUpdatedAsync(Guid scheduleId, string mealName, int currentBookings, int maxCapacity, CancellationToken cancellationToken = default);
    Task BroadcastTokenDispensedAsync(string bookingReference, string mealName, string dispensedBy, DateTime dispensedAt, CancellationToken cancellationToken = default);
}

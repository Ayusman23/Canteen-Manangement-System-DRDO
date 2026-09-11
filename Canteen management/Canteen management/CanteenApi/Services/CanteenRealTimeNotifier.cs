using Canteen.Application.Common.Interfaces;
using CanteenApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CanteenApi.Services;

public class CanteenRealTimeNotifier : ICanteenRealTimeNotifier
{
    private readonly IHubContext<CanteenHub> _hubContext;
    private readonly ILogger<CanteenRealTimeNotifier> _logger;

    public CanteenRealTimeNotifier(IHubContext<CanteenHub> hubContext, ILogger<CanteenRealTimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastMealAvailabilityUpdatedAsync(
        Guid scheduleId,
        string mealName,
        int currentBookings,
        int maxCapacity,
        CancellationToken cancellationToken = default)
    {
        var remaining = Math.Max(0, maxCapacity - currentBookings);
        _logger.LogInformation("SignalR Broadcasting MealAvailabilityUpdated: Schedule={ScheduleId}, Remaining={Remaining}/{Max}",
            scheduleId, remaining, maxCapacity);

        await _hubContext.Clients.All.SendAsync("MealAvailabilityUpdated", new
        {
            ScheduleId = scheduleId,
            MealName = mealName,
            CurrentBookings = currentBookings,
            MaxCapacity = maxCapacity,
            RemainingCapacity = remaining,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task BroadcastTokenDispensedAsync(
        string bookingReference,
        string mealName,
        string dispensedBy,
        DateTime dispensedAt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SignalR Broadcasting TokenDispensed: Token={Token}, Meal={Meal}", bookingReference, mealName);

        await _hubContext.Clients.All.SendAsync("TokenDispensed", new
        {
            BookingReference = bookingReference,
            MealName = mealName,
            DispensedBy = dispensedBy,
            DispensedAt = dispensedAt
        }, cancellationToken);
    }
}

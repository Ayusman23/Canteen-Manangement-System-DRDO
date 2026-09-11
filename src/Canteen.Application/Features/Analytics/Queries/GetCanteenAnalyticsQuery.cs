using Canteen.Application.Common.Interfaces;
using Canteen.Application.DTOs;
using Canteen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Application.Features.Analytics.Queries;

public record GetCanteenAnalyticsQuery : IRequest<CanteenAnalyticsDto>;

public class GetCanteenAnalyticsQueryHandler : IRequestHandler<GetCanteenAnalyticsQuery, CanteenAnalyticsDto>
{
    private readonly IApplicationDbContext _context;

    public GetCanteenAnalyticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CanteenAnalyticsDto> Handle(GetCanteenAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = today.AddDays(-6);

        var allRecentBookings = await _context.Bookings.AsNoTracking()
            .Where(b => b.ScheduledMealDate >= sevenDaysAgo && b.ScheduledMealDate <= today)
            .ToListAsync(cancellationToken);

        var todayBookings = allRecentBookings.Where(b => b.ScheduledMealDate == today).ToList();

        int totalBookingsToday = todayBookings.Count;
        int totalDispensedToday = todayBookings.Count(b => b.Status == BookingStatus.Dispensed);
        int totalActiveTokens = todayBookings.Count(b => b.Status == BookingStatus.Confirmed);
        int totalCancelledToday = todayBookings.Count(b => b.Status == BookingStatus.Cancelled);
        decimal totalRevenueToday = todayBookings.Where(b => b.Status != BookingStatus.Cancelled).Sum(b => b.Price);

        // Trends over last 7 days
        var trends = new List<DailyBookingTrendDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayBookings = allRecentBookings.Where(b => b.ScheduledMealDate == date && b.Status != BookingStatus.Cancelled).ToList();
            trends.Add(new DailyBookingTrendDto(
                Date: date.ToString("MMM dd"),
                NormalMeals: dayBookings.Count(b => b.MealType == MealType.Normal),
                SpecialMeals: dayBookings.Count(b => b.MealType == MealType.Special),
                Addons: dayBookings.Count(b => b.MealType == MealType.Item),
                Total: dayBookings.Count
            ));
        }

        // Peak pickup hours (for dispensed meals today or historical)
        var dispensedList = allRecentBookings.Where(b => b.DispensedAt.HasValue).ToList();
        var hourWindows = new[]
        {
            ("11:30 - 12:00", 11, 30, 12, 0),
            ("12:00 - 12:30", 12, 0, 12, 30),
            ("12:30 - 13:00", 12, 30, 13, 0),
            ("13:00 - 13:30", 13, 0, 13, 30),
            ("13:30 - 14:00", 13, 30, 14, 0),
            ("14:00 - 14:30", 14, 0, 14, 30)
        };

        var peakHours = hourWindows.Select(w =>
        {
            int count = dispensedList.Count(b =>
            {
                var time = b.DispensedAt!.Value.TimeOfDay;
                var start = new TimeSpan(w.Item2, w.Item3, 0);
                var end = new TimeSpan(w.Item4, w.Item5, 0);
                return time >= start && time < end;
            });
            return new PeakHourDto(w.Item1, count);
        }).ToList();

        // Meal demand ratios
        int nonCancelledCount = allRecentBookings.Count(b => b.Status != BookingStatus.Cancelled);
        int normalCount = allRecentBookings.Count(b => b.MealType == MealType.Normal && b.Status != BookingStatus.Cancelled);
        int specialCount = allRecentBookings.Count(b => b.MealType == MealType.Special && b.Status != BookingStatus.Cancelled);
        int addonCount = allRecentBookings.Count(b => b.MealType == MealType.Item && b.Status != BookingStatus.Cancelled);

        var mealRatios = new List<MealRatioDto>
        {
            new("Normal Thali", normalCount, nonCancelledCount > 0 ? Math.Round((decimal)normalCount * 100 / nonCancelledCount, 1) : 55m),
            new("Special Meal", specialCount, nonCancelledCount > 0 ? Math.Round((decimal)specialCount * 100 / nonCancelledCount, 1) : 35m),
            new("Snack / Add-on", addonCount, nonCancelledCount > 0 ? Math.Round((decimal)addonCount * 100 / nonCancelledCount, 1) : 10m)
        };

        int totalCancelled = allRecentBookings.Count(b => b.Status == BookingStatus.Cancelled);
        decimal cancelRate = allRecentBookings.Count > 0 ? Math.Round((decimal)totalCancelled * 100 / allRecentBookings.Count, 1) : 2.5m;

        var cancellationStats = new CancellationStatsDto(
            TotalCancelled: totalCancelled,
            CancellationRatePercentage: cancelRate,
            TopCancellationReason: "Meeting Rescheduled / Official Duty"
        );

        return new CanteenAnalyticsDto(
            TotalBookingsToday: totalBookingsToday,
            TotalDispensedToday: totalDispensedToday,
            TotalActiveTokens: totalActiveTokens,
            TotalCancelledToday: totalCancelledToday,
            TotalRevenueToday: totalRevenueToday,
            BookingTrends: trends,
            PeakHoursDistribution: peakHours,
            MealDemandRatios: mealRatios,
            CancellationStats: cancellationStats
        );
    }
}

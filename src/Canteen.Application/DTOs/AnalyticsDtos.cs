namespace Canteen.Application.DTOs;

public record CanteenAnalyticsDto(
    int TotalBookingsToday,
    int TotalDispensedToday,
    int TotalActiveTokens,
    int TotalCancelledToday,
    decimal TotalRevenueToday,
    List<DailyBookingTrendDto> BookingTrends,
    List<PeakHourDto> PeakHoursDistribution,
    List<MealRatioDto> MealDemandRatios,
    CancellationStatsDto CancellationStats
);

public record DailyBookingTrendDto(
    string Date,
    int NormalMeals,
    int SpecialMeals,
    int Addons,
    int Total
);

public record PeakHourDto(
    string HourWindow, // e.g. "12:00 - 12:30", "12:30 - 13:00"
    int PickupsCount
);

public record MealRatioDto(
    string Name,
    int Value,
    decimal Percentage
);

public record CancellationStatsDto(
    int TotalCancelled,
    decimal CancellationRatePercentage,
    string TopCancellationReason
);

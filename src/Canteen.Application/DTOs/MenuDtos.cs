using Canteen.Domain.Enums;

namespace Canteen.Application.DTOs;

public record MenuItemDto(
    Guid Id,
    string Name,
    string Description,
    MealType Category,
    decimal BasePrice,
    bool IsVegetarian,
    bool IsActive
);

public record DailyMenuScheduleDto(
    Guid Id,
    DateOnly Date,
    string DayOfWeek,
    MealType MealType,
    Guid MenuItemId,
    string MenuItemName,
    string Description,
    decimal Price,
    int MaxCapacity,
    int CurrentBookingsCount,
    int RemainingCapacity,
    string CutoffTime,
    bool IsActive,
    bool IsCutoffPassed
);

public record WeeklyMenuDayDto(
    string Day,
    DateOnly Date,
    DailyMenuScheduleDto? NormalMeal,
    DailyMenuScheduleDto? SpecialMeal,
    DailyMenuScheduleDto? AddonItem
);

// Backwards-compatible DTO matching original frontend menu.json schema
public record LegacyMenuDayDto(
    string Day,
    LegacyMealDto? Normal,
    LegacyMealDto? Special,
    LegacyItemDto? Item
);

public record LegacyMealDto(string Meal, decimal Price, Guid? ScheduleId = null, int Remaining = 0);
public record LegacyItemDto(string Name, decimal Price, Guid? ScheduleId = null, int Remaining = 0);

public record CreateMenuItemRequest(
    string Name,
    string Description,
    MealType Category,
    decimal BasePrice,
    bool IsVegetarian
);

public record UpdateScheduleCapacityRequest(
    Guid ScheduleId,
    int NewCapacity,
    decimal? NewPrice = null,
    string? NewCutoffTime = null
);

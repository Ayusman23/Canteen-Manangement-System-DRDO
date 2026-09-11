using Canteen.Application.Common.Interfaces;
using Canteen.Application.DTOs;
using Canteen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Application.Features.Menu.Queries;

public record GetWeeklyMenuScheduleQuery(DateOnly? StartDate = null) : IRequest<List<WeeklyMenuDayDto>>;
public record GetLegacyWeeklyMenuQuery : IRequest<List<LegacyMenuDayDto>>;

public class GetWeeklyMenuScheduleQueryHandler : 
    IRequestHandler<GetWeeklyMenuScheduleQuery, List<WeeklyMenuDayDto>>,
    IRequestHandler<GetLegacyWeeklyMenuQuery, List<LegacyMenuDayDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWeeklyMenuScheduleQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WeeklyMenuDayDto>> Handle(GetWeeklyMenuScheduleQuery request, CancellationToken cancellationToken)
    {
        var baseDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        // Find Monday of the current week
        int diff = (7 + (baseDate.DayOfWeek - DayOfWeek.Monday)) % 7;
        var mondayDate = baseDate.AddDays(-1 * diff);
        var sundayDate = mondayDate.AddDays(6);

        var schedules = await _context.DailyMenuSchedules
            .Include(s => s.MenuItem)
            .Where(s => s.Date >= mondayDate && s.Date <= sundayDate && s.IsActive)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.MealType)
            .ToListAsync(cancellationToken);

        // If no dated schedules exist yet for this week, fetch template schedules by DayOfWeek
        if (schedules.Count == 0)
        {
            schedules = await _context.DailyMenuSchedules
                .Include(s => s.MenuItem)
                .Where(s => s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.MealType)
                .ToListAsync(cancellationToken);
        }

        var result = new List<WeeklyMenuDayDto>();
        var daysOrder = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        for (int i = 0; i < 7; i++)
        {
            var targetDay = daysOrder[i];
            var targetDate = mondayDate.AddDays(i);

            var daySchedules = schedules.Where(s => s.Date == targetDate || (s.Date == default && s.DayOfWeek == targetDay)).ToList();

            var normal = MapScheduleDto(daySchedules.FirstOrDefault(s => s.MealType == MealType.Normal), targetDate);
            var special = MapScheduleDto(daySchedules.FirstOrDefault(s => s.MealType == MealType.Special), targetDate);
            var addon = MapScheduleDto(daySchedules.FirstOrDefault(s => s.MealType == MealType.Item), targetDate);

            result.Add(new WeeklyMenuDayDto(
                Day: targetDay.ToString(),
                Date: targetDate,
                NormalMeal: normal,
                SpecialMeal: special,
                AddonItem: addon
            ));
        }

        return result;
    }

    public async Task<List<LegacyMenuDayDto>> Handle(GetLegacyWeeklyMenuQuery request, CancellationToken cancellationToken)
    {
        var weekly = await Handle(new GetWeeklyMenuScheduleQuery(), cancellationToken);

        return weekly.Select(w => new LegacyMenuDayDto(
            Day: w.Day,
            Normal: w.NormalMeal != null ? new LegacyMealDto(w.NormalMeal.MenuItemName, w.NormalMeal.Price, w.NormalMeal.Id, w.NormalMeal.RemainingCapacity) : null,
            Special: w.SpecialMeal != null ? new LegacyMealDto(w.SpecialMeal.MenuItemName, w.SpecialMeal.Price, w.SpecialMeal.Id, w.SpecialMeal.RemainingCapacity) : null,
            Item: w.AddonItem != null ? new LegacyItemDto(w.AddonItem.MenuItemName, w.AddonItem.Price, w.AddonItem.Id, w.AddonItem.RemainingCapacity) : null
        )).ToList();
    }

    private static DailyMenuScheduleDto? MapScheduleDto(Canteen.Domain.Entities.DailyMenuSchedule? s, DateOnly actualDate)
    {
        if (s == null) return null;
        var now = DateTime.UtcNow;
        var isCutoff = actualDate == DateOnly.FromDateTime(now) && now.TimeOfDay > s.CutoffTime;
        var remaining = Math.Max(0, s.MaxCapacity - s.CurrentBookingsCount);

        return new DailyMenuScheduleDto(
            Id: s.Id,
            Date: actualDate,
            DayOfWeek: actualDate.DayOfWeek.ToString(),
            MealType: s.MealType,
            MenuItemId: s.MenuItemId,
            MenuItemName: s.MenuItem?.Name ?? $"{s.MealType} Meal",
            Description: s.MenuItem?.Description ?? string.Empty,
            Price: s.Price,
            MaxCapacity: s.MaxCapacity,
            CurrentBookingsCount: s.CurrentBookingsCount,
            RemainingCapacity: remaining,
            CutoffTime: $"{s.CutoffTime:hh\\:mm}",
            IsActive: s.IsActive,
            IsCutoffPassed: isCutoff
        );
    }
}

using Canteen.Application.Common.Exceptions;
using Canteen.Application.Common.Interfaces;
using Canteen.Application.DTOs;
using Canteen.Domain.Entities;
using Canteen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Application.Features.Menu.Commands;

public record CreateMenuItemCommand(
    string Name,
    string Description,
    MealType Category,
    decimal BasePrice,
    bool IsVegetarian
) : IRequest<MenuItemDto>;

public record UpdateScheduleCapacityCommand(
    Guid ScheduleId,
    int NewCapacity,
    decimal? NewPrice,
    string? NewCutoffTime
) : IRequest<DailyMenuScheduleDto>;

public class ManageMenuCommandHandler : 
    IRequestHandler<CreateMenuItemCommand, MenuItemDto>,
    IRequestHandler<UpdateScheduleCapacityCommand, DailyMenuScheduleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICanteenRealTimeNotifier _notifier;

    public ManageMenuCommandHandler(IApplicationDbContext context, ICanteenRealTimeNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task<MenuItemDto> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            BasePrice = request.BasePrice,
            IsVegetarian = request.IsVegetarian,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return new MenuItemDto(
            item.Id,
            item.Name,
            item.Description,
            item.Category,
            item.BasePrice,
            item.IsVegetarian,
            item.IsActive
        );
    }

    public async Task<DailyMenuScheduleDto> Handle(UpdateScheduleCapacityCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.DailyMenuSchedules
            .Include(s => s.MenuItem)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

        if (schedule == null)
        {
            throw new NotFoundException("DailyMenuSchedule", request.ScheduleId);
        }

        if (request.NewCapacity < schedule.CurrentBookingsCount)
        {
            throw new AppException($"New capacity ({request.NewCapacity}) cannot be lower than existing confirmed bookings ({schedule.CurrentBookingsCount}).", 400);
        }

        schedule.MaxCapacity = request.NewCapacity;
        if (request.NewPrice.HasValue && request.NewPrice > 0)
        {
            schedule.Price = request.NewPrice.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.NewCutoffTime) && TimeSpan.TryParse(request.NewCutoffTime, out var parsedCutoff))
        {
            schedule.CutoffTime = parsedCutoff;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast updated capacity via SignalR
        await _notifier.BroadcastMealAvailabilityUpdatedAsync(
            schedule.Id,
            schedule.MenuItem?.Name ?? $"{schedule.MealType}",
            schedule.CurrentBookingsCount,
            schedule.MaxCapacity,
            cancellationToken
        );

        var remaining = Math.Max(0, schedule.MaxCapacity - schedule.CurrentBookingsCount);
        var now = DateTime.UtcNow;
        var isCutoff = schedule.Date == DateOnly.FromDateTime(now) && now.TimeOfDay > schedule.CutoffTime;

        return new DailyMenuScheduleDto(
            schedule.Id,
            schedule.Date,
            schedule.DayOfWeek.ToString(),
            schedule.MealType,
            schedule.MenuItemId,
            schedule.MenuItem?.Name ?? "",
            schedule.MenuItem?.Description ?? "",
            schedule.Price,
            schedule.MaxCapacity,
            schedule.CurrentBookingsCount,
            remaining,
            $"{schedule.CutoffTime:hh\\:mm}",
            schedule.IsActive,
            isCutoff
        );
    }
}

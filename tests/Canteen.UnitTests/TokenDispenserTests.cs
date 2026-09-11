using Canteen.Application.Common.Exceptions;
using Canteen.Application.Common.Interfaces;
using Canteen.Application.Features.Bookings.Commands;
using Canteen.Domain.Entities;
using Canteen.Domain.Enums;
using Canteen.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Canteen.UnitTests;

public class TokenDispenserTests
{
    private CanteenDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CanteenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CanteenDbContext(options);
    }

    [Fact]
    public async Task DispenseMeal_Should_Succeed_On_Valid_Token()
    {
        using var context = CreateDbContext();
        var notifierMock = new Mock<ICanteenRealTimeNotifier>();

        var schedule = new DailyMenuSchedule
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DayOfWeek = DateTime.UtcNow.DayOfWeek,
            MealType = MealType.Normal,
            Price = 30m,
            MaxCapacity = 100
        };
        context.DailyMenuSchedules.Add(schedule);

        var token = "DRDO-20260911-9999";
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = token,
            UserName = "Engineer Rao",
            ScheduleId = schedule.Id,
            Schedule = schedule,
            MealType = MealType.Normal,
            MealName = "Normal Thali",
            Price = 30m,
            ScheduledMealDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = BookingStatus.Confirmed,
            QrCodeHash = "HASH123"
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var handler = new DispenseMealCommandHandler(context, notifierMock.Object);
        var result = await handler.Handle(new DispenseMealCommand(token, "Kitchen Chef 1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.BookingReference.Should().Be(token);

        var updated = await context.Bookings.FindAsync(booking.Id);
        updated!.Status.Should().Be(BookingStatus.Dispensed);
        updated.DispensedAt.Should().NotBeNull();
        updated.DispensedByUserId.Should().Be("Kitchen Chef 1");

        notifierMock.Verify(n => n.BroadcastTokenDispensedAsync(token, "Normal Thali", "Kitchen Chef 1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispenseMeal_Should_Throw_Conflict_When_Token_Is_Already_Dispensed()
    {
        using var context = CreateDbContext();
        var notifierMock = new Mock<ICanteenRealTimeNotifier>();

        var schedule = new DailyMenuSchedule
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DayOfWeek = DateTime.UtcNow.DayOfWeek,
            MealType = MealType.Special,
            Price = 50m,
            MaxCapacity = 100
        };
        context.DailyMenuSchedules.Add(schedule);

        var token = "DRDO-20260911-8888";
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = token,
            UserName = "Commander Sharma",
            ScheduleId = schedule.Id,
            Schedule = schedule,
            MealType = MealType.Special,
            MealName = "Special Meal",
            Price = 50m,
            ScheduledMealDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = BookingStatus.Dispensed, // Already dispensed
            DispensedAt = DateTime.UtcNow.AddMinutes(-10),
            DispensedByUserId = "Chef Operator A"
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var handler = new DispenseMealCommandHandler(context, notifierMock.Object);
        var act = async () => await handler.Handle(new DispenseMealCommand(token, "Chef Operator B"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == "ALREADY_DISPENSED");
    }
}

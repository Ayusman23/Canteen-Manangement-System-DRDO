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

public class BookingConcurrencyTests
{
    private CanteenDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CanteenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new CanteenDbContext(options);
        return context;
    }

    [Fact]
    public async Task CreateBooking_Should_Enforce_Strict_Capacity_And_Prevent_Overselling()
    {
        // Arrange
        using var context = CreateDbContext();
        var notifierMock = new Mock<ICanteenRealTimeNotifier>();

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Special Biryani Thali",
            BasePrice = 50m,
            Category = MealType.Special
        };
        context.MenuItems.Add(menuItem);

        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var schedule = new DailyMenuSchedule
        {
            Id = Guid.NewGuid(),
            Date = targetDate,
            DayOfWeek = targetDate.DayOfWeek,
            MealType = MealType.Special,
            MenuItemId = menuItem.Id,
            Price = 50m,
            MaxCapacity = 2, // Strict capacity of only 2 meals
            CurrentBookingsCount = 0,
            CutoffTime = new TimeSpan(23, 59, 59)
        };
        context.DailyMenuSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var handler = new CreateBookingCommandHandler(context, notifierMock.Object);

        // Act & Assert
        // Booking 1 - Succeeds
        var res1 = await handler.Handle(new CreateBookingCommand("Scientist Alpha", "Special", null, targetDate, schedule.Id, null, null, null), CancellationToken.None);
        res1.Token.Should().StartWith("DRDO-");

        // Booking 2 - Succeeds
        var res2 = await handler.Handle(new CreateBookingCommand("Scientist Beta", "Special", null, targetDate, schedule.Id, null, null, null), CancellationToken.None);
        res2.Token.Should().StartWith("DRDO-");

        // Booking 3 - MUST fail with CapacityExceededException
        var act = async () => await handler.Handle(new CreateBookingCommand("Scientist Gamma", "Special", null, targetDate, schedule.Id, null, null, null), CancellationToken.None);
        await act.Should().ThrowAsync<CapacityExceededException>()
            .WithMessage("*maximum booking capacity*");

        // Verify schedule state in DB
        var updatedSchedule = await context.DailyMenuSchedules.FindAsync(schedule.Id);
        updatedSchedule!.CurrentBookingsCount.Should().Be(2);

        // Verify total confirmed bookings is exactly 2
        var totalBookings = await context.Bookings.CountAsync(b => b.ScheduleId == schedule.Id);
        totalBookings.Should().Be(2);
    }

    [Fact]
    public async Task CancelBooking_Should_Decrement_Capacity_And_Allow_New_Booking()
    {
        // Arrange
        using var context = CreateDbContext();
        var notifierMock = new Mock<ICanteenRealTimeNotifier>();

        var menuItem = new MenuItem { Id = Guid.NewGuid(), Name = "Roti Dal", BasePrice = 30m, Category = MealType.Normal };
        context.MenuItems.Add(menuItem);

        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var schedule = new DailyMenuSchedule
        {
            Id = Guid.NewGuid(),
            Date = targetDate,
            DayOfWeek = targetDate.DayOfWeek,
            MealType = MealType.Normal,
            MenuItemId = menuItem.Id,
            Price = 30m,
            MaxCapacity = 1, // Capacity 1
            CurrentBookingsCount = 0,
            CutoffTime = new TimeSpan(23, 59, 59)
        };
        context.DailyMenuSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var createHandler = new CreateBookingCommandHandler(context, notifierMock.Object);
        var cancelHandler = new CancelBookingCommandHandler(context, notifierMock.Object);

        // Book slot 1
        var bookingRes = await createHandler.Handle(new CreateBookingCommand("Officer One", "Normal", null, targetDate, schedule.Id, null, null, null), CancellationToken.None);

        // Verify capacity is full
        var fullAct = async () => await createHandler.Handle(new CreateBookingCommand("Officer Two", "Normal", null, targetDate, schedule.Id, null, null, null), CancellationToken.None);
        await fullAct.Should().ThrowAsync<CapacityExceededException>();

        // Cancel slot 1
        var cancelRes = await cancelHandler.Handle(new CancelBookingCommand(bookingRes.Token, "Official Tour", "Officer One"), CancellationToken.None);
        cancelRes.Should().BeTrue();

        // Now Officer Two should be able to book successfully!
        var retryBooking = await createHandler.Handle(new CreateBookingCommand("Officer Two", "Normal", null, targetDate, schedule.Id, null, null, null), CancellationToken.None);
        retryBooking.Token.Should().StartWith("DRDO-");
    }
}

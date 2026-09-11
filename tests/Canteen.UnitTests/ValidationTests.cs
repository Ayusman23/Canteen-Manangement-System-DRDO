using Canteen.Application.Features.Bookings.Commands;
using Canteen.Application.Features.Bookings.Validators;
using FluentAssertions;
using Xunit;

namespace Canteen.UnitTests;

public class ValidationTests
{
    private readonly CreateBookingCommandValidator _validator = new();

    [Fact]
    public async Task CreateBookingCommandValidator_Should_Fail_When_Name_Is_Empty()
    {
        var command = new CreateBookingCommand("", "Normal", "Monday", null, null, null, null, null);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateBookingCommandValidator_Should_Fail_When_MealType_Is_Invalid()
    {
        var command = new CreateBookingCommand("John Doe", "Pizza", "Monday", null, null, null, null, null);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MealType");
    }

    [Fact]
    public async Task CreateBookingCommandValidator_Should_Pass_For_Valid_Request()
    {
        var command = new CreateBookingCommand("Dr. Vikram Sarabhai", "Special", "Friday", null, null, null, null, null);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}

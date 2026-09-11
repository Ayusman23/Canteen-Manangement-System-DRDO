using Canteen.Application.Features.Bookings.Commands;
using FluentValidation;

namespace Canteen.Application.Features.Bookings.Validators;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Full Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.MealType)
            .NotEmpty().WithMessage("Meal type is required.")
            .Must(m => m.Equals("Normal", StringComparison.OrdinalIgnoreCase) ||
                       m.Equals("Special", StringComparison.OrdinalIgnoreCase) ||
                       m.Equals("Item", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Meal type must be 'Normal', 'Special', or 'Item'.");
    }
}

public class DispenseMealCommandValidator : AbstractValidator<DispenseMealCommand>
{
    public DispenseMealCommandValidator()
    {
        RuleFor(x => x.TokenOrQr)
            .NotEmpty().WithMessage("Booking Token or QR code payload is required.");

        RuleFor(x => x.DispensedBy)
            .NotEmpty().WithMessage("Kitchen Operator identifier is required.");
    }
}

public class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.BookingReference)
            .NotEmpty().WithMessage("Booking Reference is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.");
    }
}

using Canteen.Application.Features.Menu.Commands;
using FluentValidation;

namespace Canteen.Application.Features.Menu.Validators;

public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BasePrice).GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}

public class UpdateScheduleCapacityCommandValidator : AbstractValidator<UpdateScheduleCapacityCommand>
{
    public UpdateScheduleCapacityCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.NewCapacity).GreaterThanOrEqualTo(0).WithMessage("Capacity cannot be negative.");
    }
}

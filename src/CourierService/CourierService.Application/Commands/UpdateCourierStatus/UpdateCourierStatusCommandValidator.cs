using FluentValidation;
using System.Linq;

namespace CourierService.Application.Commands.UpdateCourierStatus;

public class UpdateCourierStatusCommandValidator : AbstractValidator<UpdateCourierStatusCommand>
{
    public UpdateCourierStatusCommandValidator()
    {
        RuleFor(x => x.CourierId)
            .NotEmpty().WithMessage("Courier ID is required");

        RuleFor(x => x.Model).SetValidator(new UpdateCourierModelValidator());
    }
}

public class UpdateCourierModelValidator : AbstractValidator<Models.UpdateCourierModel>
{
    private static readonly List<int> ValidStatuses = new() { 0, 1, 2, 3 }; // Active, OnDuty, OffDuty, Inactive

    public UpdateCourierModelValidator()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required")
            .Must(x => x.HasValue && ValidStatuses.Contains(x.Value))
            .WithMessage("Status must be a valid courier status code");
    }
}

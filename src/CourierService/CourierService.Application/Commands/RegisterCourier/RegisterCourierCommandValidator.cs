using FluentValidation;

namespace CourierService.Application.Commands.RegisterCourier;

public class RegisterCourierCommandValidator : AbstractValidator<RegisterCourierCommand>
{
    public RegisterCourierCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateCourierModelValidator());
    }
}

public class CreateCourierModelValidator : AbstractValidator<Models.CreateCourierModel>
{
    public CreateCourierModelValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format (E.164)")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("Document number is required")
            .MaximumLength(50).WithMessage("Document number cannot exceed 50 characters");
    }
}

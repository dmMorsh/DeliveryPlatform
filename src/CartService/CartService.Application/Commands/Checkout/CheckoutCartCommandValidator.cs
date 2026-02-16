using FluentValidation;

namespace CartService.Application.Commands.Checkout;

public class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.FromAddress)
            .NotEmpty()
            .WithMessage("FromAddress is required")
            .Length(3, 500)
            .WithMessage("FromAddress must be between 3 and 500 characters");

        RuleFor(x => x.ToAddress)
            .NotEmpty()
            .WithMessage("ToAddress is required")
            .Length(3, 500)
            .WithMessage("ToAddress must be between 3 and 500 characters");

        RuleFor(x => x.FromLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("FromLatitude must be between -90 and 90");

        RuleFor(x => x.FromLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("FromLongitude must be between -180 and 180");

        RuleFor(x => x.ToLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("ToLatitude must be between -90 and 90");

        RuleFor(x => x.ToLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("ToLongitude must be between -180 and 180");

        RuleFor(x => x.WeightGrams)
            .GreaterThan(0)
            .WithMessage("WeightGrams must be greater than 0");

        RuleFor(x => x.CostCents)
            .GreaterThan(0)
            .WithMessage("CostCents must be greater than 0");

        RuleFor(x => x.Currency)
            .Length(3, 6)
            .WithMessage("Currency code must be 3-6 characters (ISO 4217 or crypto)")
            .When(x => !string.IsNullOrEmpty(x.Currency));

        RuleFor(x => x.CourierNote)
            .MaximumLength(500)
            .WithMessage("CourierNote must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.CourierNote));
    }
}

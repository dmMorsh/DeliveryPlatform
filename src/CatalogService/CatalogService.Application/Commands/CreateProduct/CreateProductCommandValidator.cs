using FluentValidation;

namespace CatalogService.Application.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Product description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.PriceCents)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3, 6).WithMessage("Currency code must be 3-6 characters (ISO 4217 or crypto)");

        RuleFor(x => x.WeightGrams)
            .GreaterThan(0).WithMessage("Weight must be greater than 0");
    }
}

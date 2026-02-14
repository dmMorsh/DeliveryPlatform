using FluentValidation;

namespace CatalogService.Application.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Product description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.PriceCents)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .When(x => x.PriceCents.HasValue);

        RuleFor(x => x.Currency)
            .Length(3, 6).WithMessage("Currency code must be 3-6 characters (ISO 4217 or crypto)")
            .When(x => !string.IsNullOrEmpty(x.Currency));
    }
}

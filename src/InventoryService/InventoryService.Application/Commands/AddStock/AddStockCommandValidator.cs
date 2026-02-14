using FluentValidation;

namespace InventoryService.Application.Commands.AddStock;

public class AddStockCommandValidator : AbstractValidator<AddStockCommand>
{
    public AddStockCommandValidator()
    {
        RuleFor(x => x.Models)
            .NotEmpty().WithMessage("Stock items are required")
            .Must(x => x.Length > 0).WithMessage("At least one stock item is required");

        RuleForEach(x => x.Models).SetValidator(new SimpleStockItemModelValidator());
    }
}

public class SimpleStockItemModelValidator : AbstractValidator<Models.SimpleStockItemModel>
{
    public SimpleStockItemModelValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}

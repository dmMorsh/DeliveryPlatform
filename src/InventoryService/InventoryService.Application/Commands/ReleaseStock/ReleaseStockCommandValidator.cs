using FluentValidation;

namespace InventoryService.Application.Commands.ReleaseStock;

public class ReleaseStockCommandValidator : AbstractValidator<ReleaseStockCommand>
{
    public ReleaseStockCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");

        RuleFor(x => x.ReleaseStockModels)
            .NotEmpty().WithMessage("Stock items to release are required")
            .Must(x => x.Length > 0).WithMessage("At least one stock item is required");

        RuleForEach(x => x.ReleaseStockModels).SetValidator(new SimpleStockItemModelValidator());
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

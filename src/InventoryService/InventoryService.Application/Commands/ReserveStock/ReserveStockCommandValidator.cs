using FluentValidation;

namespace InventoryService.Application.Commands.ReserveStock;

public class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");

        RuleFor(x => x.ReserveStockModels)
            .NotEmpty().WithMessage("Stock items to reserve are required")
            .Must(x => x.Length > 0).WithMessage("At least one stock item is required");

        RuleForEach(x => x.ReserveStockModels).SetValidator(new SimpleStockItemModelValidator());
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

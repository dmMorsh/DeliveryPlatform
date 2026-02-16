using FluentValidation;

namespace OrderService.Application.Commands.UpdateReservedStock;

public class UpdateReservedStockCommandValidator : AbstractValidator<UpdateReservedStockCommand>
{
    public UpdateReservedStockCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Items collection cannot be empty");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage("ProductId is required");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than 0");
            });
    }
}

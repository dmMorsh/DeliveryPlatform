using FluentValidation;

namespace OrderService.Application.Commands.UpdateOrder;

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.CourierNote)
            .MaximumLength(500)
            .WithMessage("CourierNote must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.CourierNote));
    }
}

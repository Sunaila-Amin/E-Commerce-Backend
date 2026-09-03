using ECommerce.Business.DTOs.Order;
using FluentValidation;

namespace ECommerce.Business.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items)
            .Must(i => i.Quantity > 0).WithMessage("Item quantity must be greater than zero.")
            .Must(i => i.ProductId > 0).WithMessage("A valid product id is required.");
    }
}

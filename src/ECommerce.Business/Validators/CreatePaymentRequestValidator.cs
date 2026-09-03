using ECommerce.Business.DTOs.Payment;
using FluentValidation;

namespace ECommerce.Business.Validators;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("A valid order id is required.");

        RuleFor(x => x.Method)
            .IsInEnum().WithMessage("A valid payment method is required.");
    }
}

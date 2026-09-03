using ECommerce.Business.DTOs.Category;
using FluentValidation;

namespace ECommerce.Business.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(120);
    }
}

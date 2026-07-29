using CliniKa.Application.DTOs.Allergy;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class CreateAllergyRequestValidator : AbstractValidator<CreateAllergyRequest>
    {
        public CreateAllergyRequestValidator()
        {
            RuleFor(x => x.AllergenName)
                .NotEmpty().WithMessage("Allergen name is required.")
                .MaximumLength(200).WithMessage("Allergen name cannot exceed 200 characters.");

            RuleFor(x => x.AllergyType)
                .MaximumLength(100).WithMessage("Allergy type cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.AllergyType));
        }
    }
}

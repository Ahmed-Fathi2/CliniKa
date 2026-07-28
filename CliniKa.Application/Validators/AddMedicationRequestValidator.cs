using CliniKa.Application.DTOs.Patient;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class AddMedicationRequestValidator : AbstractValidator<AddMedicationRequest>
    {
        public AddMedicationRequestValidator()
        {
            RuleFor(x => x.DrugName)
                .NotEmpty().WithMessage("Drug name is required.")
                .MaximumLength(100).WithMessage("Drug name cannot exceed 100 characters.");

            RuleFor(x => x.Dosage)
                .MaximumLength(50).WithMessage("Dosage cannot exceed 50 characters.")
                .When(x => x.Dosage != null);

            RuleFor(x => x.Frequency)
                .MaximumLength(50).WithMessage("Frequency cannot exceed 50 characters.")
                .When(x => x.Frequency != null);
        }
    }
}

using CliniKa.Application.DTOs.Sessions;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class PrescribeMedicationRequestValidator : AbstractValidator<PrescribeMedicationRequest>
    {
        public PrescribeMedicationRequestValidator()
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

            RuleFor(x => x.Duration)
                .MaximumLength(50).WithMessage("Duration cannot exceed 50 characters.")
                .When(x => x.Duration != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}

using System;
using System.Linq;
using CliniKa.Application.DTOs.Patient;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class AddAllergyRequestValidator : AbstractValidator<AddAllergyRequest>
    {
        private static readonly string[] ValidSeverities = { "Mild", "Moderate", "Severe" };

        public AddAllergyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.AllergyId.HasValue || !string.IsNullOrWhiteSpace(x.RawAllergenName))
                .WithMessage("Either AllergyId or RawAllergenName must be provided.")
                .WithName("AllergyId/RawAllergenName");

            RuleFor(x => x.AllergyId)
                .GreaterThan(0).WithMessage("AllergyId must be greater than 0.")
                .When(x => x.AllergyId.HasValue);

            RuleFor(x => x.RawAllergenName)
                .MaximumLength(200).WithMessage("RawAllergenName cannot exceed 200 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.RawAllergenName));

            RuleFor(x => x.Severity)
                .Must(s => ValidSeverities.Contains(s!, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Severity must be one of: Mild, Moderate, Severe.")
                .When(x => !string.IsNullOrWhiteSpace(x.Severity));
        }
    }
}

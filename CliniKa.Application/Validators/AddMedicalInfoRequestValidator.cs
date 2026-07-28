using System;
using System.Linq;
using CliniKa.Application.DTOs.Patient;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class AddMedicalInfoRequestValidator : AbstractValidator<AddMedicalInfoRequest>
    {
        private static readonly string[] ValidGenders = { "Male", "Female" };
        private static readonly string[] ValidBloodTypes = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

        public AddMedicalInfoRequestValidator()
        {
            RuleFor(x => x.Gender)
                .Must(g => ValidGenders.Contains(g!, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Gender must be 'Male' or 'Female'.")
                .When(x => !string.IsNullOrWhiteSpace(x.Gender));

            RuleFor(x => x.BloodType)
                .Must(b => ValidBloodTypes.Contains(b!))
                .WithMessage("BloodType must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")
                .When(x => !string.IsNullOrWhiteSpace(x.BloodType));

            RuleFor(x => x.NationalId)
                .MaximumLength(20).WithMessage("NationalId cannot exceed 20 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.NationalId));

            RuleFor(x => x.DateOfBirth)
                .LessThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("DateOfBirth cannot be in the future.")
                .When(x => x.DateOfBirth.HasValue);

            RuleForEach(x => x.Medications)
                .SetValidator(new AddMedicationRequestValidator())
                .When(x => x.Medications != null);

            RuleForEach(x => x.Diseases)
                .SetValidator(new AddDiseaseRequestValidator())
                .When(x => x.Diseases != null);

            RuleForEach(x => x.Allergies)
                .SetValidator(new AddAllergyRequestValidator())
                .When(x => x.Allergies != null);
        }
    }
}

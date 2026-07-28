using System;
using CliniKa.Application.DTOs.Patient;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class AddDiseaseRequestValidator : AbstractValidator<AddDiseaseRequest>
    {
        public AddDiseaseRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.DiseaseId.HasValue || !string.IsNullOrWhiteSpace(x.RawDiseaseName))
                .WithMessage("Either DiseaseId or RawDiseaseName must be provided.")
                .WithName("DiseaseId/RawDiseaseName");

            RuleFor(x => x.DiseaseId)
                .GreaterThan(0).WithMessage("DiseaseId must be greater than 0.")
                .When(x => x.DiseaseId.HasValue);

            RuleFor(x => x.RawDiseaseName)
                .MaximumLength(200).WithMessage("RawDiseaseName cannot exceed 200 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.RawDiseaseName));

            RuleFor(x => x.DiagnosedDate)
                .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("DiagnosedDate cannot be in the future.")
                .When(x => x.DiagnosedDate.HasValue);
        }
    }
}

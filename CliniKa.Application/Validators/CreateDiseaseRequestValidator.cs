using CliniKa.Application.DTOs.Disease;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class CreateDiseaseRequestValidator : AbstractValidator<CreateDiseaseRequest>
    {
        public CreateDiseaseRequestValidator()
        {
            RuleFor(x => x.DiseaseCode)
                .NotEmpty().WithMessage("Disease code is required.")
                .MaximumLength(50).WithMessage("Disease code cannot exceed 50 characters.");

            RuleFor(x => x.DiseaseName)
                .NotEmpty().WithMessage("Disease name is required.")
                .MaximumLength(200).WithMessage("Disease name cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}

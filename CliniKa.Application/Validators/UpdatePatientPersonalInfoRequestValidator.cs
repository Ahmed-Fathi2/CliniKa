using CliniKa.Application.DTOs.Patient;
using FluentValidation;

namespace CliniKa.Application.Validators
{
    public class UpdatePatientPersonalInfoRequestValidator : AbstractValidator<UpdatePatientPersonalInfoRequest>
    {
        public UpdatePatientPersonalInfoRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
                .MinimumLength(2).WithMessage("First name must be at least 2 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
                .MinimumLength(2).WithMessage("Last name must be at least 2 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^(010|011|012|015)[0-9]{8}$")
                .WithMessage("Phone number must be a valid Egyptian mobile number (e.g. 01012345678).");
        }
    }
}

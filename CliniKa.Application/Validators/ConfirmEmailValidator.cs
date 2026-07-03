using FluentValidation;
using CliniKa.Application.DTOs;

namespace CliniKa.Application.Validators
{
    public class ConfirmEmailValidator : AbstractValidator<ConfirmEmailDto>
    {
        public ConfirmEmailValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required.");
        }
    }
}

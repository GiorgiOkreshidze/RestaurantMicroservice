using FluentValidation;
using Restaurant.Application.DTOs.Auth;

namespace Restaurant.Application.Validations.Auth
{
    public class UserLoginRequestValidator : AbstractValidator<LoginDto>
    {
        public UserLoginRequestValidator()
        {
            RuleFor(x => x.Email)
           .NotEmpty().WithMessage("Email is required.")
           .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}

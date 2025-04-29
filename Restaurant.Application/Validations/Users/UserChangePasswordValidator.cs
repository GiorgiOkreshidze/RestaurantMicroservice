using FluentValidation;
using Restaurant.Application.DTOs.Users;

namespace Restaurant.Application.Validations.Users;

public class UserChangePasswordValidator : AbstractValidator<UpdatePasswordRequest>
{
    public UserChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(16).WithMessage("Password cannot exceed 16 characters.")
            .Matches(@"(?=.*[A-Z])").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"(?=.*[a-z])").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"(?=.*\d)").WithMessage("Password must contain at least one number.")
            .Matches(@"(?=.*[!@#$%^&*()_+\-=[\]{};':""\\|,.<>/?])").WithMessage("Password must contain at least one special character.");
    }
}
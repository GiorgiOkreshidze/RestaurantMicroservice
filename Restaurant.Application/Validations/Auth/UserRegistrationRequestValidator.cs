using FluentValidation;
using Restaurant.Application.DTOs.Auth;

namespace Restaurant.Application.Validations.Auth;

public class UserRegistrationRequestValidator : AbstractValidator<RegisterDto>
{
    public UserRegistrationRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\-']+$").WithMessage("First name can contain only Latin letters, hyphens, and apostrophes.");
        
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\-']+$").WithMessage("Last name can contain only Latin letters, hyphens, and apostrophes.");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Email must be in the format test@email.com.");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(16).WithMessage("Password cannot exceed 16 characters.")
            .Matches(@"(?=.*[A-Z])").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"(?=.*[a-z])").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"(?=.*\d)").WithMessage("Password must contain at least one number.")
            .Matches(@"(?=.*[!@#$%^&*()_+\-=[\]{};':""\\|,.<>/?])").WithMessage("Password must contain at least one special character.");
    }
}
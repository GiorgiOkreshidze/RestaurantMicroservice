using FluentValidation;
using Restaurant.Application.DTOs.Users;

namespace Restaurant.Application.Validations.Users;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\-']+$").WithMessage("First name can contain only Latin letters, hyphens, and apostrophes.");
        
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\-']+$").WithMessage("Last name can contain only Latin letters, hyphens, and apostrophes.");  
    }
}
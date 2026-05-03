using FluentValidation;
using VTOS.Application.Features.Auth.Commands;

namespace VTOS.Application.Features.Auth.Validators;

/// <summary>
/// Validator for RegisterCommand (NO phone validation).
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(25).WithMessage("Password must not exceed 25 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character (e.g., *, $, +, @, !, #)");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters");

        RuleFor(x => x.AcceptedTerms)
            .Equal(true).WithMessage("You must accept the terms of use");

        RuleFor(x => x.TermsVersion)
            .NotEmpty().WithMessage("Terms version is required")
            .MaximumLength(32).WithMessage("Terms version must not exceed 32 characters");
    }
}

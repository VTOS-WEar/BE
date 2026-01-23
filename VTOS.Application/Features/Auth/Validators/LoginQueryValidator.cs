using FluentValidation;
using VTOS.Application.Features.Auth.Queries;

namespace VTOS.Application.Features.Auth.Validators;

/// <summary>
/// Validator for LoginQuery.
/// </summary>
public class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

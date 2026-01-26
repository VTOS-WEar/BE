using FluentValidation;
using VTOS.Application.Features.Auth.Commands;

namespace VTOS.Application.Features.Auth.Validators;

/// <summary>
/// Validator for ChangePasswordCommand.
/// Uses same password rules as UC-01 Register and UC-03 Reset Password.
/// </summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OTP)
            .NotEmpty().WithMessage("OTP is required.")
            .Length(6).WithMessage("OTP must be 6 digits.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(25).WithMessage("Password must not exceed 25 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character (e.g., *, $, +, @, !, #).");

        RuleFor(x => x)
            .Must(x => x.CurrentPassword != x.NewPassword)
            .WithMessage("New password must be different from current password.");
    }
}

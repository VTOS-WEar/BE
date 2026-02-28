using FluentValidation;
using VTOS.Application.Features.Users.Commands;

namespace VTOS.Application.Features.Users.Validators;

/// <summary>
/// Validator for SubmitVerificationCommand
/// </summary>
public class SubmitVerificationValidator : AbstractValidator<SubmitVerificationCommand>
{
    public SubmitVerificationValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone number must be a valid E.164 format (e.g., +1234567890).")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Avatar)
            .Must(file => file == null || file.Length <= 5 * 1024 * 1024)
            .WithMessage("Avatar file size cannot exceed 5MB.")
            .When(x => x.Avatar != null);
    }
}

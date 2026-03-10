using FluentValidation;
using VTOS.Application.Features.Schools.Commands;

namespace VTOS.Application.Features.Schools.Validators;

/// <summary>
/// Validator for UpdateSchoolProfileCommand (UC-42).
/// </summary>
public class UpdateSchoolProfileCommandValidator : AbstractValidator<UpdateSchoolProfileCommand>
{
    public UpdateSchoolProfileCommandValidator()
    {
        RuleFor(x => x.SchoolName)
            .MaximumLength(255)
            .When(x => x.SchoolName != null)
            .WithMessage("School name must not exceed 255 characters.");

        RuleFor(x => x.LogoURL)
            .MaximumLength(2_097_152) // ~2 MB base64 data URI
            .When(x => x.LogoURL != null)
            .WithMessage("Logo file is too large. Please use an image under 2 MB.");

        RuleFor(x => x.ContactInfo)
            .MaximumLength(500)
            .When(x => x.ContactInfo != null)
            .WithMessage("Contact info must not exceed 500 characters.");

        RuleFor(x => x.Level)
            .MaximumLength(50)
            .When(x => x.Level != null)
            .WithMessage("Level must not exceed 50 characters.");

        // At least one field must be provided
        RuleFor(x => x)
            .Must(x => x.SchoolName != null || x.LogoURL != null || x.ContactInfo != null || x.Level != null)
            .WithMessage("At least one field must be provided for update.");
    }
}

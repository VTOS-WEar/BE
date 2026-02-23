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
            .MaximumLength(500)
            .When(x => x.LogoURL != null)
            .WithMessage("Logo URL must not exceed 500 characters.");

        RuleFor(x => x.ContactInfo)
            .MaximumLength(500)
            .When(x => x.ContactInfo != null)
            .WithMessage("Contact info must not exceed 500 characters.");

        // At least one field must be provided
        RuleFor(x => x)
            .Must(x => x.SchoolName != null || x.LogoURL != null || x.ContactInfo != null)
            .WithMessage("At least one field must be provided for update.");
    }
}

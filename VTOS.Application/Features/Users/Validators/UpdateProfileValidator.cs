using FluentValidation;
using VTOS.Application.Features.Users.Commands;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Users.Validators;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.DOB)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("DOB cannot be in the future.")
            .LessThanOrEqualTo(DateTime.Today.AddYears(-18))
            .WithMessage("User must be at least 18 years old")
            .When(x => x.DOB.HasValue);

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gender must be a valid value (Male, Female, or Other)")
            .When(x => x.Gender.HasValue);
    }
}


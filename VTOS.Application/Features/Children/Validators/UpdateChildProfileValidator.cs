using FluentValidation;
using VTOS.Application.Features.Children.Commands;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Children.Validators;

public class UpdateChildProfileValidator : AbstractValidator<UpdateChildProfileCommand>
{
    public UpdateChildProfileValidator()
    {
        // ChildId must be provided
        RuleFor(x => x.ChildId)
            .NotEqual(Guid.Empty)
            .WithMessage("ChildId is required.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.DOB)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("DOB cannot be in the future.")
            .LessThanOrEqualTo(DateTime.Today.AddYears(-7))
            .WithMessage("User must be at least 7 years old")
            .When(x => x.DOB.HasValue);

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gender must be a valid value (Male, Female, or Other)")
            .When(x => x.Gender.HasValue);

        // HeightCm is optional; if provided must be in a sensible range for children
        RuleFor(x => x.HeightCm)
            .InclusiveBetween(30, 220)
            .When(x => x.HeightCm.HasValue)
            .WithMessage("HeightCm must be between 30 and 220 cm.");

        // WeightKg is optional; if provided must be in a sensible range
        RuleFor(x => x.WeightKg)
            .InclusiveBetween(1, 200)
            .When(x => x.WeightKg.HasValue)
            .WithMessage("WeightKg must be between 1 and 200 kg.");

    }
}


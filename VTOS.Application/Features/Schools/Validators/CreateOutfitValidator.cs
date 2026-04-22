using FluentValidation;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

public class CreateOutfitValidator : AbstractValidator<CreateOutfitCommand>
{
    public CreateOutfitValidator()
    {
        RuleFor(x => x.OutfitName)
            .NotEmpty().WithMessage("Outfit name is required.")
            .MaximumLength(255).WithMessage("Outfit name must not exceed 255 characters.");

        RuleFor(x => x.OutfitType)
            .IsInEnum().WithMessage("Invalid outfit type. Valid values: Uniform=1, Sportswear=2, Accessory=3, Other=4.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.MainImageURL)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .When(x => x.MainImageURL != null);
    }
}

using FluentValidation;
using VTOS.Application.Features.Schools.Commands;

namespace VTOS.Application.Features.Schools.Validators;

/// <summary>
/// Validation rules for PublishCampaignCommand (UC-44).
/// </summary>
public class PublishCampaignCommandValidator : AbstractValidator<PublishCampaignCommand>
{
    public PublishCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignName)
            .NotEmpty().WithMessage("Campaign name is required.")
            .MaximumLength(200).WithMessage("Campaign name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x.Outfits)
            .NotEmpty().WithMessage("At least one outfit must be included in the campaign.");

        RuleForEach(x => x.Outfits).ChildRules(outfit =>
        {
            outfit.RuleFor(o => o.OutfitId)
                .NotEmpty().WithMessage("Outfit ID is required.");

            outfit.RuleFor(o => o.CampaignPrice)
                .GreaterThan(0).WithMessage("Campaign price must be greater than 0.");

            outfit.RuleFor(o => o.MaxQuantity)
                .GreaterThan(0).WithMessage("Max quantity must be greater than 0.")
                .When(o => o.MaxQuantity.HasValue);
        });
    }
}

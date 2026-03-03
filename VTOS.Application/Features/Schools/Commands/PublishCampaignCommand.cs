using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Input for a single outfit entry in a campaign.
/// ProviderID is nullable — school can pick provider later.
/// </summary>
public record CampaignOutfitInput(
    Guid OutfitId,
    Guid? ProviderId,
    decimal CampaignPrice,
    int? MaxQuantity
);

/// <summary>
/// UC-44: Publish (or save as draft) a uniform pre-order campaign for the school.
/// Creates Campaign + CampaignOutfit entries atomically.
/// </summary>
public record PublishCampaignCommand(
    Guid UserId,
    string CampaignName,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<CampaignOutfitInput> Outfits,
    bool SaveAsDraft = false
);

public interface IPublishCampaignCommandHandler
{
    Task<Result<PublishCampaignResponseDto>> HandleAsync(PublishCampaignCommand command, CancellationToken ct = default);
}


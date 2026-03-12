namespace VTOS.Application.Features.Public.DTOs;

public record PublicCampaignOutfitDto(
    Guid CampaignOutfitId,
    Guid OutfitId,
    string OutfitName,
    string? MainImageUrl,
    decimal CampaignPrice,
    int? MaxQuantity
);

public record PublicSchoolSummaryDto(
    Guid Id,
    string SchoolName,
    string? LogoURL
);

public record PublicCampaignDetailResponse(
    Guid CampaignId,
    string CampaignName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    PublicSchoolSummaryDto School,
    IEnumerable<PublicCampaignOutfitDto> Outfits
);

namespace VTOS.Application.Features.Public.DTOs;

public record OutfitSchoolDto(
    Guid SchoolId,
    string SchoolName,
    string? LogoURL
);

public record OutfitCampaignOptionDto(
    Guid CampaignId,
    string CampaignName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    Guid CampaignOutfitId,
    decimal CampaignPrice,
    int? MaxQuantity,
    IEnumerable<ProductVariantDto> Variants
);

public record OutfitDetailResponse(
    Guid OutfitId,
    string OutfitName,
    string? Description,
    decimal Price,
    string OutfitType,
    string? MainImageURL,
    bool IsAvailable,
    bool IsCustomizable,
    OutfitSchoolDto School,
    IEnumerable<ProductVariantDto> Variants,
    SizeChartDto? SizeChart,
    IEnumerable<OutfitCampaignOptionDto> CampaignOptions,
    IEnumerable<string> Categories,
    decimal AverageRating,
    int FeedbackCount,
    IEnumerable<ReviewDto> Reviews
);

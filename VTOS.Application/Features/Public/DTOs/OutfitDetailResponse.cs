namespace VTOS.Application.Features.Public.DTOs;

public record OutfitSchoolDto(
    Guid SchoolId,
    string SchoolName,
    string? LogoURL
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
    IEnumerable<string> Categories,
    decimal AverageRating,
    int FeedbackCount
);

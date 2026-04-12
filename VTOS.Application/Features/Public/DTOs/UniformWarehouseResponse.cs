using System;
using System.Collections.Generic;

namespace VTOS.Application.Features.Public.DTOs;

public record CampaignSummaryDto(
    Guid CampaignId,
    string CampaignName,
    Guid SchoolId,
    string SchoolName,
    string? SchoolLogoUrl,
    DateTime StartDate,
    DateTime EndDate,
    string Status
);

public record FeaturedOutfitDto(
    Guid OutfitId,
    string OutfitName,
    decimal Price,
    string? MainImageUrl,
    string SchoolName,
    Guid SchoolId,
    double AverageRating
);

public record UniformWarehouseResponse(
    IEnumerable<CampaignSummaryDto> ActiveCampaigns,
    IEnumerable<FeaturedOutfitDto> FeaturedOutfits,
    IEnumerable<UniformSearchResult> AllOutfits,
    int TotalOutfits
);

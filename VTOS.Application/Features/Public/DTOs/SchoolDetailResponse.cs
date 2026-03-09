namespace VTOS.Application.Features.Public.DTOs;

public record SchoolCampaignDto(
    Guid CampaignId,
    string CampaignName,
    DateTime StartDate,
    DateTime EndDate,
    string Status
);

public record SchoolDetailResponse(
    Guid SchoolId,
    string SchoolName,
    string? LogoURL,
    string? ContactInfo,
    int OutfitCount,
    IEnumerable<SchoolCampaignDto> ActiveCampaigns
);

namespace VTOS.Application.Features.Public.DTOs;

public record SchoolDto(
    Guid SchoolId,
    string SchoolName,
    string? LogoURL,
    string? ContactInfo,
    int OutfitCount
);

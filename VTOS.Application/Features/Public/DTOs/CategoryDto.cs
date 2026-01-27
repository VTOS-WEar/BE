namespace VTOS.Application.Features.Public.DTOs;

public record CategoryDto(
    Guid CategoryId,
    string CategoryName,
    int OutfitCount
);

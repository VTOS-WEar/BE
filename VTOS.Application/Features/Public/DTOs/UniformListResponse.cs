namespace VTOS.Application.Features.Public.DTOs;

public record UniformDto(
    Guid OutfitId,
    string OutfitName,
    decimal Price,
    string OutfitType,
    string? MainImageURL,
    bool IsAvailable,
    IEnumerable<string> CategoryNames,
    decimal AverageRating,
    int FeedbackCount
);

public record UniformListResponse(
    IEnumerable<UniformDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

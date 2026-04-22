namespace VTOS.Application.Features.Providers.DTOs;

public record SubmitProviderRatingResponse(
    Guid ProviderRatingId,
    Guid OrderId,
    Guid ProviderId,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record ProviderRatingItemDto(
    Guid ProviderRatingId,
    Guid OrderId,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    string ParentName
);

public class ProviderRatingsResponse
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalCompletedOrders { get; set; }
    public List<ProviderRatingItemDto> Items { get; set; } = new();
}

public record ProviderRankingItemDto(
    Guid ProviderId,
    string ProviderName,
    decimal AverageRating,
    int TotalRatings,
    int TotalCompletedOrders
);

public class ProviderRankingResponse
{
    public Guid SchoolId { get; set; }
    public List<ProviderRankingItemDto> Items { get; set; } = new();
}

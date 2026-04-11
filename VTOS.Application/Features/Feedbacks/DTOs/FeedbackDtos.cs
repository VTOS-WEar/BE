namespace VTOS.Application.Features.Feedbacks.DTOs;

public record SubmitFeedbackRequest(
    Guid UserId,
    Guid OrderItemId,
    int Rating,
    string? Comment
);

public record SubmitFeedbackResponse(
    Guid FeedbackId,
    Guid OrderItemId,
    int Rating,
    string? Comment,
    DateTime Timestamp
);

// Parent feedback view - for listing feedbacks with product info
public record ParentFeedbackDto(
    Guid FeedbackId,
    Guid OrderItemId,
    Guid CampaignId,
    string CampaignName,
    Guid OutfitId,
    string OutfitName,
    string OutfitImageUrl,
    int? Rating,           // null if not yet rated
    string? Comment,
    DateTime? FeedbackTimestamp,
    DateTime OrderDate,
    decimal OutfitPrice,
    string OutfitType,
    string Size,
    int Quantity
);

public record RatingCountDto(string Label, int Count);

public record ParentFeedbacksResponse(
    List<ParentFeedbackDto> Items,
    int Total,
    int Page,
    int PageSize,
    List<CampaignFilterDto> Campaigns,
    List<RatingCountDto> RatingCounts
);

public record CampaignFilterDto(
    Guid CampaignId,
    string CampaignName,
    int Count
);

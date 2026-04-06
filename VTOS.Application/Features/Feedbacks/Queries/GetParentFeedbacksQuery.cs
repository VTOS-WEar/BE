namespace VTOS.Application.Features.Feedbacks.Queries;

public record GetParentFeedbacksQuery(
    Guid ParentId,
    Guid? CampaignId = null,
    bool? HasRating = null, // null = all, true = rated, false = not rated
    int Page = 1,
    int PageSize = 10
);

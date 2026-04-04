namespace VTOS.Application.Features.Feedbacks.Commands;

public record SubmitFeedbackCommand(
    Guid UserId,
    Guid ProductVariantId,
    Guid CampaignId,
    int Rating,
    string? Comment
);

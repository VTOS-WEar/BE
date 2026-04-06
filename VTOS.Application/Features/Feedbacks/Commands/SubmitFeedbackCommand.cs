namespace VTOS.Application.Features.Feedbacks.Commands;

public record SubmitFeedbackCommand(
    Guid UserId,
    Guid OrderItemId,
    int Rating,
    string? Comment
);

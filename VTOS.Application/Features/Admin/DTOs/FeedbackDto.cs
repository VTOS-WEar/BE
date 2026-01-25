namespace VTOS.Application.Features.Admin.DTOs;

public record FeedbackDto(
    Guid Id,
    string UserEmail,
    string? Comment,
    int Rating,
    DateTime Timestamp,
    string ModerationStatus
);

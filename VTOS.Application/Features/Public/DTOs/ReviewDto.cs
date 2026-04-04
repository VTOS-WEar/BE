namespace VTOS.Application.Features.Public.DTOs;

public record ReviewDto(
    Guid FeedbackId,
    int Rating,
    string? Comment,
    DateTime Timestamp,
    string UserName,
    string? UserAvatarUrl
);

namespace VTOS.Application.Features.Users.DTOs;

/// <summary>
/// Response DTO for verification submission.
/// Contains updated user profile information.
/// </summary>
public record SubmitVerificationResponse(
    Guid Id,
    string Email,
    string FullName,
    string Phone,
    string Avatar,
    string Gender,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime VerifiedAt,
    string Message
);

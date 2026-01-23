namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Response DTO for successful registration.
/// </summary>
public record RegisterResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Message
);

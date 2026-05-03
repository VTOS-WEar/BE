namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for user login.
/// </summary>
public record LoginRequest(
    string Email,
    string Password,
    string TurnstileToken
);

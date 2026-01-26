namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for forgot password.
/// </summary>
public record ForgotPasswordRequest(string Email);

/// <summary>
/// Response DTO for forgot password.
/// Always returns same message regardless of email existence (security).
/// </summary>
public record ForgotPasswordResponse(string Message);

namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for resetting password with token.
/// </summary>
public record ResetPasswordRequest(string Token, string NewPassword);

/// <summary>
/// Response DTO for password reset.
/// </summary>
public record ResetPasswordResponse(string Message);

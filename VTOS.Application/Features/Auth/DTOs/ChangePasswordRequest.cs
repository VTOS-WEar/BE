namespace VTOS.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for changing password with OTP verification.
/// </summary>
public record ChangePasswordRequest(
    string OTP,
    string CurrentPassword,
    string NewPassword
);

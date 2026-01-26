namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command to change password with OTP verification.
/// </summary>
public record ChangePasswordCommand(
    Guid UserId,
    string OTP,
    string CurrentPassword,
    string NewPassword
);

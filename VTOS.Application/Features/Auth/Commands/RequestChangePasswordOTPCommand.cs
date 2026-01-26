namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command to request OTP for password change.
/// UserId will be extracted from JWT claims in the controller.
/// </summary>
public record RequestChangePasswordOTPCommand(Guid UserId);

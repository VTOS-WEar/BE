namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command to reset password with token.
/// </summary>
public record ResetPasswordCommand(string Token, string NewPassword);

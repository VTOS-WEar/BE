namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command to request password reset.
/// </summary>
public record ForgotPasswordCommand(string Email);

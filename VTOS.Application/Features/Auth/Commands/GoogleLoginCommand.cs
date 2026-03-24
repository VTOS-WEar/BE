namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command to login/register via Google OAuth.
/// </summary>
public record GoogleLoginCommand(string IdToken);

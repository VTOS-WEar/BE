using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command for user registration. RoleName defaults to "Parent".
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string TurnstileToken,
    string? RoleName = null,
    bool AcceptedTerms = false,
    string? TermsVersion = null
);

/// <summary>
/// Handler interface for RegisterCommand.
/// </summary>
public interface IRegisterCommandHandler
{
    Task<Result<RegisterResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default);
}

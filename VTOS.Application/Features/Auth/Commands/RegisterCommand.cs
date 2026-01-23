using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command for user registration (NO phone - collected after first login).
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string FullName
);

/// <summary>
/// Handler interface for RegisterCommand.
/// </summary>
public interface IRegisterCommandHandler
{
    Task<Result<RegisterResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default);
}

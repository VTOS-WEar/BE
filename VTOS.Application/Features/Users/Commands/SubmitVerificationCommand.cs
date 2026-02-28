using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;
using Microsoft.AspNetCore.Http;

namespace VTOS.Application.Features.Users.Commands;

/// <summary>
/// Command to update user profile with avatar, name, and phone.
/// </summary>
public record SubmitVerificationCommand(
    Guid UserId,
    string? FullName,
    string? Phone,
    IFormFile? Avatar
);

public interface ISubmitVerificationCommandHandler
{
    Task<Result<SubmitVerificationResponse>> HandleAsync(
        SubmitVerificationCommand command,
        CancellationToken cancellationToken = default);
}

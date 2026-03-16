using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;

namespace VTOS.Application.Features.Providers.Commands;

/// <summary>
/// Command to update the current provider's profile.
/// </summary>
public record UpdateProviderProfileCommand(
    Guid UserId,
    string? ProviderName,
    string? ContactPersonName,
    string? Phone,
    string? Email,
    string? Address
);

public interface IUpdateProviderProfileCommandHandler
{
    Task<Result<ProviderProfileDto>> HandleAsync(UpdateProviderProfileCommand command, CancellationToken ct = default);
}

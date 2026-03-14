using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;

namespace VTOS.Application.Features.Providers.Queries;

/// <summary>
/// Query to get the current provider's profile.
/// </summary>
public record GetProviderProfileQuery(Guid UserId);

public interface IGetProviderProfileQueryHandler
{
    Task<Result<ProviderProfileDto>> HandleAsync(GetProviderProfileQuery query, CancellationToken ct = default);
}

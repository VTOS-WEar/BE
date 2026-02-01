using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Queries;

/// <summary>
/// Query for user GetProfile.
/// </summary>
public record GetProfileQuery(
    Guid Id
);

/// <summary>
/// Handler interface for GetProfileQuery.
/// </summary>
public interface IGetProfileQueryHandler
{
    Task<Result<GetProfileResponse>> HandleAsync(GetProfileQuery query, CancellationToken cancellationToken = default);
}

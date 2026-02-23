using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-42: Get school profile for current user.
/// </summary>
public record GetSchoolProfileQuery(Guid UserId);

public interface IGetSchoolProfileQueryHandler
{
    Task<Result<SchoolProfileDto>> HandleAsync(GetSchoolProfileQuery query, CancellationToken ct = default);
}

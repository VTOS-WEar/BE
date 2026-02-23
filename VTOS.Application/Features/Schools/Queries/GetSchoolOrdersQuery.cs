using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-45: Get orders associated with the school.
/// </summary>
public record GetSchoolOrdersQuery(Guid SchoolId, int Page = 1, int PageSize = 10, string? Status = null);

public interface IGetSchoolOrdersQueryHandler
{
    Task<Result<SchoolOrderListResponse>> HandleAsync(GetSchoolOrdersQuery query, CancellationToken ct = default);
}

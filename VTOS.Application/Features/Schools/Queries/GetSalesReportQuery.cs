using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-49: View sales reports for the school.
/// </summary>
public record GetSalesReportQuery(Guid SchoolId, DateTime? FromDate = null, DateTime? ToDate = null);

public interface IGetSalesReportQueryHandler
{
    Task<Result<SalesReportDto>> HandleAsync(GetSalesReportQuery query, CancellationToken ct = default);
}

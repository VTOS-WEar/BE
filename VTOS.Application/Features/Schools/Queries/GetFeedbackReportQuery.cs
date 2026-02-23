using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-50: View feedback reports for the school.
/// </summary>
public record GetFeedbackReportQuery(Guid SchoolId, DateTime? FromDate = null, DateTime? ToDate = null);

public interface IGetFeedbackReportQueryHandler
{
    Task<Result<FeedbackReportDto>> HandleAsync(GetFeedbackReportQuery query, CancellationToken ct = default);
}

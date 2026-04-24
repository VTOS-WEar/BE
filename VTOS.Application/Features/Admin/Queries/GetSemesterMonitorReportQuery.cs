using VTOS.Application.Common;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetSemesterMonitorReportQuery(Guid SemesterPublicationId);

public interface IGetSemesterMonitorReportQueryHandler
{
    Task<Result<SemesterMonitorReportDto>> HandleAsync(
        GetSemesterMonitorReportQuery query,
        CancellationToken ct = default);
}

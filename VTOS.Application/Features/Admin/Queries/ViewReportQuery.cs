using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record ViewReportQuery(
    string ReportType,  // "Order", "Revenue", "SchoolPerformance", "ProviderPerformance"
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    Guid? SchoolId = null
);

public interface IViewReportQueryHandler
{
    Task<dynamic> HandleAsync(
        ViewReportQuery query,
        CancellationToken cancellationToken);
}

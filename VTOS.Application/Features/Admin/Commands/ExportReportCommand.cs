using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ExportReportCommand(
    string ReportType,  // "Order", "Revenue", "SchoolPerformance", "ProviderPerformance"
    string ExportFormat,  // "CSV", "Excel", "PDF"
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    Guid? SchoolId = null
);

public interface IExportReportCommandHandler
{
    Task<Result<byte[]>> HandleAsync(
        ExportReportCommand command,
        CancellationToken cancellationToken);
}

using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ExportSchoolActivityLogsCommand(
    Guid SchoolId,
    DateTime? DateFrom = null,
    DateTime? DateTo = null
);

public interface IExportSchoolActivityLogsCommandHandler
{
    Task<Result<byte[]>> HandleAsync(
        ExportSchoolActivityLogsCommand command,
        CancellationToken cancellationToken);
}

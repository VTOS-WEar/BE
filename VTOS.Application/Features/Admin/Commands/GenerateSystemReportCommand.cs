using VTOS.Application.Common;
using VTOS.Application.Features.Admin.Commands.DTOs;

namespace VTOS.Application.Features.Admin.Commands;

public record GenerateSystemReportCommand(
    string ReportFrequency  // "Daily", "Weekly", "Monthly"
);

public interface IGenerateSystemReportCommandHandler
{
    Task<Result<SystemReportResponse>> HandleAsync(
        GenerateSystemReportCommand command,
        CancellationToken cancellationToken);
}

using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ConfigureDefaultSizeChartCommand(
    Guid SizeChartId
);

public interface IConfigureDefaultSizeChartCommandHandler
{
    Task<Result<string>> HandleAsync(
        ConfigureDefaultSizeChartCommand command,
        CancellationToken cancellationToken);
}

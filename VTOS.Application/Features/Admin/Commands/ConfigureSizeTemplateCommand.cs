using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ConfigureSizeTemplateCommand(
    string ChartName,
    string? Description,
    string Unit = "cm"
);

public interface IConfigureSizeTemplateCommandHandler
{
    Task<Result<Guid>> HandleAsync(
        ConfigureSizeTemplateCommand command,
        CancellationToken cancellationToken);
}

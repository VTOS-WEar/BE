using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ConfigureAITryOnSettingsCommand(
    string? ModelVersion = null,
    string? ImageResolution = null,
    int? MaxUploadFileSizeMB = null
);

public interface IConfigureAITryOnSettingsCommandHandler
{
    Task<Result<string>> HandleAsync(
        ConfigureAITryOnSettingsCommand command,
        CancellationToken cancellationToken);
}

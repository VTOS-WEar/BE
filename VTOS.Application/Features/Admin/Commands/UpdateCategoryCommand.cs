using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record UpdateCategoryCommand(
    Guid CategoryId,
    string CategoryName
);

public interface IUpdateCategoryCommandHandler
{
    Task<Result<string>> HandleAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken);
}

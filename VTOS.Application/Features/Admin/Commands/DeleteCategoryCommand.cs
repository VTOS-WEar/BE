using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record DeleteCategoryCommand(Guid CategoryId);

public interface IDeleteCategoryCommandHandler
{
    Task<Result<string>> HandleAsync(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken);
}

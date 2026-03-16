using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record AddCategoryCommand(
    string CategoryName
);

public interface IAddCategoryCommandHandler
{
    Task<Result<Guid>> HandleAsync(
        AddCategoryCommand command,
        CancellationToken cancellationToken);
}

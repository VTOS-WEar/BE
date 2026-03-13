using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Command to soft-delete a product variant (sets IsDeleted = true).
/// </summary>
public record DeleteVariantCommand(Guid UserId, Guid OutfitId, Guid VariantId);

public interface IDeleteVariantCommandHandler
{
    Task<Result<bool>> HandleAsync(DeleteVariantCommand command, CancellationToken ct = default);
}

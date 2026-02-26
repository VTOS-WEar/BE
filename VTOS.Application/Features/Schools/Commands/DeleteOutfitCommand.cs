using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Command to soft-delete an outfit (sets IsDeleted = true).
/// </summary>
public record DeleteOutfitCommand(Guid UserId, Guid OutfitId);

public interface IDeleteOutfitCommandHandler
{
    Task<Result<bool>> HandleAsync(DeleteOutfitCommand command, CancellationToken ct = default);
}

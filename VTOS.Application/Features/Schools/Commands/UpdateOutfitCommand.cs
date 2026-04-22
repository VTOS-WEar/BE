using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Command to update an existing outfit. All fields are optional (partial update).
/// </summary>
public record UpdateOutfitCommand(
    Guid UserId,
    Guid OutfitId,
    string? OutfitName,
    string? Description,
    string? MaterialType,
    OutfitType? OutfitType,
    string? MainImageURL,
    Guid? SizeChartID,
    bool? IsAvailable,
    bool? IsCustomizable
);

public interface IUpdateOutfitCommandHandler
{
    Task<Result<OutfitDto>> HandleAsync(UpdateOutfitCommand command, CancellationToken ct = default);
}

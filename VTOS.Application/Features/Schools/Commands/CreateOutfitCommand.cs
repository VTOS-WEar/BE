using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Command to create a new outfit for a school.
/// </summary>
public record CreateOutfitCommand(
    Guid UserId,
    string OutfitName,
    string? Description,
    string? MaterialType,
    OutfitType OutfitType,
    Guid? CategoryId,
    string? MainImageURL,
    Guid? SizeChartID,
    bool IsCustomizable
);

public interface ICreateOutfitCommandHandler
{
    Task<Result<OutfitDto>> HandleAsync(CreateOutfitCommand command, CancellationToken ct = default);
}

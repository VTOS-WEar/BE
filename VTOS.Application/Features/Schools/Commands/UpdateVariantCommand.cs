using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Command to update an existing product variant.
/// All fields are optional (partial update).
/// </summary>
public record UpdateVariantCommand(
    Guid UserId,
    Guid OutfitId,
    Guid VariantId,
    string? Size,
    decimal? Price,
    int? StockQuantity,
    string? ColorVariant,
    string? MaterialType,
    string? SKUCode
);

public interface IUpdateVariantCommandHandler
{
    Task<Result<ProductVariantDto>> HandleAsync(UpdateVariantCommand command, CancellationToken ct = default);
}

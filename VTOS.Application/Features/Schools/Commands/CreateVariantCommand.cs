using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Command to create a new product variant for an outfit.
/// </summary>
public record CreateVariantCommand(
    Guid UserId,
    Guid OutfitId,
    string Size,
    decimal Price,
    int StockQuantity,
    string? ColorVariant,
    string? MaterialType,
    string? SKUCode
);

public interface ICreateVariantCommandHandler
{
    Task<Result<ProductVariantDto>> HandleAsync(CreateVariantCommand command, CancellationToken ct = default);
}

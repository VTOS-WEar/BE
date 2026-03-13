using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Query to get all product variants for a specific outfit.
/// </summary>
public record GetOutfitVariantsQuery(Guid UserId, Guid OutfitId);

public interface IGetOutfitVariantsQueryHandler
{
    Task<Result<List<ProductVariantDto>>> HandleAsync(GetOutfitVariantsQuery query, CancellationToken ct = default);
}

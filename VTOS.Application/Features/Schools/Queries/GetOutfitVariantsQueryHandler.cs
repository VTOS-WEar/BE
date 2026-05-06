using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Helpers;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Handler to get all product variants for an outfit owned by the current school.
/// </summary>
public class GetOutfitVariantsQueryHandler : IGetOutfitVariantsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetOutfitVariantsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<ProductVariantDto>>> HandleAsync(GetOutfitVariantsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null)
            return Result<List<ProductVariantDto>>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<List<ProductVariantDto>>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        // Verify outfit belongs to this school
        var outfit = await _db.Outfits
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<List<ProductVariantDto>>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        if (outfit.SchoolID != schoolMgr.SchoolID)
            return Result<List<ProductVariantDto>>.Failure("You do not have permission to view this outfit.", "OUTFIT_NOT_FOUND");

        var sizeDetails = await _db.SizeChartDetails
            .AsNoTracking()
            .Where(detail => detail.SizeChartID == outfit.SizeChartID)
            .Include(detail => detail.Measurements)
            .ToListAsync(ct);

        var variants = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.OutfitID == query.OutfitId && v.ProviderCatalogItemID == null && !v.IsDeleted)
            .OrderBy(v => v.Size)
            .Select(v => new ProductVariantDto
            {
                ProductVariantId = v.Id,
                OutfitId = v.OutfitID,
                Size = v.Size,
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                ColorVariant = v.ColorVariant,
                MaterialType = v.MaterialType,
                SKUCode = v.SKUCode,
                VariantImageURL = v.VariantImageURL,
            })
            .ToListAsync(ct);

        foreach (var variant in variants)
        {
            var detail = sizeDetails.FirstOrDefault(d => d.SizeLabel == variant.Size);
            variant.Measurements = VariantSizeChartSyncHelper.ToDtos(detail);
        }

        return Result<List<ProductVariantDto>>.Success(variants);
    }
}

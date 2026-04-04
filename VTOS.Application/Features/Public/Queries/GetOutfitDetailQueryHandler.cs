using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;

namespace VTOS.Application.Features.Public.Queries;

public class GetOutfitDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetOutfitDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OutfitDetailResponse?> HandleAsync(GetOutfitDetailQuery query, CancellationToken ct = default)
    {
        var outfit = await _context.Outfits
            .AsNoTracking()
            .Include(o => o.School)
            .Include(o => o.ProductVariants.Where(pv => !pv.IsDeleted))
            .Include(o => o.SizeChart)
                .ThenInclude(sc => sc!.SizeChartDetails)
            .Include(o => o.OutfitCategories)
                .ThenInclude(oc => oc.Category)
            .FirstOrDefaultAsync(o => o.Id == query.OutfitId, ct);

        if (outfit == null)
            return null;

        // Get feedbacks for this outfit across all orders
        var feedbacks = await _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.OrderItem)
                .ThenInclude(oi => oi.ProductVariant)
            .Where(f => f.OrderItem.ProductVariant.OutfitID == query.OutfitId)
            .ToListAsync(ct);

        // Calculate average rating
        var averageRating = feedbacks.Any() ? (decimal)feedbacks.Average(f => f.Rating) : 0m;

        // Build size chart DTO
        SizeChartDto? sizeChartDto = null;
        if (outfit.SizeChart != null)
        {
            var details = outfit.SizeChart.SizeChartDetails
                .Select(d => new SizeChartDetailDto(
                    d.SizeLabel,
                    d.ChestMin,
                    d.ChestMax,
                    d.WaistMin,
                    d.WaistMax,
                    d.HeightMin,
                    d.HeightMax
                ))
                .ToList();

            sizeChartDto = new SizeChartDto(
                outfit.SizeChart.Id,
                outfit.SizeChart.ChartName,
                outfit.SizeChart.Unit,
                details
            );
        }

        // Build variants
        var variants = outfit.ProductVariants
            .Select(pv => new ProductVariantDto(
                pv.Id,
                pv.Size,
                pv.ColorVariant,
                pv.MaterialType,
                pv.StockQuantity,
                pv.Price,
                pv.SKUCode,
                pv.VariantImageURL
            ))
            .ToList();

        // Build categories
        var categories = outfit.OutfitCategories
            .Select(oc => oc.Category.CategoryName)
            .ToList();

        return new OutfitDetailResponse(
            outfit.Id,
            outfit.OutfitName,
            outfit.Description,
            outfit.Price,
            outfit.OutfitType.ToString(),
            outfit.MainImageURL,
            outfit.IsAvailable,
            outfit.IsCustomizable,
            new OutfitSchoolDto(outfit.School.Id, outfit.School.SchoolName, outfit.School.LogoURL),
            variants,
            sizeChartDto,
            categories,
            Math.Round(averageRating, 1),
            feedbacks.Count
        );
    }
}

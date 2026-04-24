using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class GetCategoriesQueryHandler : IGetCategoriesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        var legacyOutfitCounts = await _context.Outfits
            .AsNoTracking()
            .Where(o => !o.IsDeleted && !o.OutfitCategories.Any())
            .GroupBy(o => o.OutfitType)
            .Select(g => new { OutfitType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OutfitType, x => x.Count, cancellationToken);

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .Select(c => new
            {
                c.Id,
                c.CategoryName,
                c.CreatedAt,
                LinkedOutfitCount = c.OutfitCategories.Count
            })
            .ToListAsync(cancellationToken);

        return categories
            .Select(c =>
            {
                var legacyCount = TryInferOutfitType(c.CategoryName, out var outfitType)
                    ? legacyOutfitCounts.GetValueOrDefault(outfitType)
                    : 0;

                return new CategoryDto(
                    c.Id,
                    c.CategoryName,
                    c.CreatedAt,
                    c.LinkedOutfitCount + legacyCount);
            })
            .ToList();
    }

    private static bool TryInferOutfitType(string categoryName, out OutfitType outfitType)
    {
        var normalized = categoryName.Trim().ToLowerInvariant();

        if (normalized is "đồng phục" or "dong phuc" or "uniform")
        {
            outfitType = OutfitType.Uniform;
            return true;
        }

        if (normalized is "đồ thể thao" or "do the thao" or "sportswear")
        {
            outfitType = OutfitType.Sportswear;
            return true;
        }

        if (normalized is "phụ kiện" or "phu kien" or "accessory")
        {
            outfitType = OutfitType.Accessory;
            return true;
        }

        if (normalized is "khác" or "khac" or "other")
        {
            outfitType = OutfitType.Other;
            return true;
        }

        outfitType = default;
        return false;
    }
}

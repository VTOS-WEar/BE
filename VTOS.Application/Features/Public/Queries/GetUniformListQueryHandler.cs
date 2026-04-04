using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;

namespace VTOS.Application.Features.Public.Queries;

public class GetUniformListQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    public GetUniformListQueryHandler(IApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<UniformListResponse?> HandleAsync(GetUniformListQuery query, CancellationToken ct = default)
    {
        var cacheKey = $"public:school:{query.SchoolId}:uniforms:p{query.Page}:s{query.PageSize}";

        if (_cache.TryGetValue(cacheKey, out UniformListResponse? cached))
            return cached;

        // Verify school exists
        var schoolExists = await _context.Schools
            .AsNoTracking()
            .AnyAsync(s => s.Id == query.SchoolId, ct);

        if (!schoolExists)
            return null;

        // Build base query — only available, non-deleted outfits for this school
        var outfitsQuery = _context.Outfits
            .AsNoTracking()
            .Where(o => o.SchoolID == query.SchoolId && !o.IsDeleted && o.IsAvailable);

        var totalCount = await outfitsQuery.CountAsync(ct);

        // Get outfits with categories
        var outfits = await outfitsQuery
            .OrderBy(o => o.OutfitName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(o => o.OutfitCategories)
                .ThenInclude(oc => oc.Category)
            .ToListAsync(ct);

        // Get campaign outfit IDs and feedback stats for these outfits
        var outfitIdMap = new Dictionary<Guid, (decimal AvgRating, int Count)>();

        foreach (var outfit in outfits)
        {
            var feedbacks = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => f.ProductVariantID == outfit.Id)
                .ToListAsync(ct);

            var avgRating = feedbacks.Any() 
                ? Math.Round((decimal)feedbacks.Average(f => f.Rating), 1)
                : 0m;

            outfitIdMap[outfit.Id] = (avgRating, feedbacks.Count);
        }

        // Build DTOs
        var items = outfits.Select(o => new UniformDto(
            o.Id,
            o.OutfitName,
            o.Price,
            o.OutfitType.ToString(),
            o.MainImageURL,
            o.IsAvailable,
            o.OutfitCategories.Select(oc => oc.Category.CategoryName).ToList(),
            outfitIdMap[o.Id].AvgRating,
            outfitIdMap[o.Id].Count
        )).ToList();

        var result = new UniformListResponse(items, totalCount, query.Page, query.PageSize);

        _cache.Set(cacheKey, result, CacheOptions);

        return result;
    }
}

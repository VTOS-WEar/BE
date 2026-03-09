using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;

namespace VTOS.Application.Features.Public.Queries;

public class GetSchoolDetailQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    public GetSchoolDetailQueryHandler(IApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<SchoolDetailResponse?> HandleAsync(GetSchoolDetailQuery query, CancellationToken ct = default)
    {
        var cacheKey = $"public:school:{query.SchoolId}";

        if (_cache.TryGetValue(cacheKey, out SchoolDetailResponse? cached))
            return cached;

        var school = await _context.Schools
            .AsNoTracking()
            .Where(s => s.Id == query.SchoolId)
            .Select(s => new SchoolDetailResponse(
                s.Id,
                s.SchoolName,
                s.LogoURL,
                s.ContactInfo,
                s.Outfits.Count(o => o.IsAvailable && !o.IsDeleted),
                s.Campaigns
                    .Where(c => c.EndDate >= DateTime.UtcNow)
                    .Select(c => new SchoolCampaignDto(
                        c.Id,
                        c.CampaignName,
                        c.StartDate,
                        c.EndDate,
                        c.Status.ToString()
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(ct);

        if (school != null)
            _cache.Set(cacheKey, school, CacheOptions);

        return school;
    }
}

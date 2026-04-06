using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;

namespace VTOS.Application.Features.Public.Queries;

public class GetSchoolsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetSchoolsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SchoolListResponse> HandleAsync(GetSchoolsQuery query, CancellationToken ct = default)
    {
        var schoolsQuery = _context.Schools.AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.ToLower();
            schoolsQuery = schoolsQuery.Where(s => s.SchoolName.ToLower().Contains(searchTerm));
        }

        // Get total count
        var totalCount = await schoolsQuery.CountAsync(ct);

        // Get paginated schools
        var schools = await schoolsQuery
            .OrderBy(s => s.SchoolName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(s => s.Outfits)
            .ToListAsync(ct);

        // Build DTOs with feedback averages
        var schoolDtos = new List<SchoolDto>();
        foreach (var school in schools)
        {
            var outfitIds = school.Outfits.Where(o => o.IsAvailable).Select(o => o.Id).ToList();
            
            // Get campaign outfit IDs for this school's outfits
            // Get average rating for this school
            var avgRating = outfitIds.Any() 
                ? await _context.Feedbacks
                    .AsNoTracking()
                    .Include(f => f.OrderItem)
                        .ThenInclude(oi => oi.ProductVariant)
                    .Where(f => outfitIds.Contains(f.OrderItem.ProductVariant.OutfitID))
                    .AverageAsync(f => (double?)f.Rating, ct)
                : null;

            schoolDtos.Add(new SchoolDto(
                school.Id,
                school.SchoolName,
                school.LogoURL,
                school.ContactInfo,
                outfitIds.Count,
                school.Level,
                avgRating
            ));
        }

        return new SchoolListResponse(schoolDtos, totalCount, query.Page, query.PageSize);
    }
}

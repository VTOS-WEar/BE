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

        // Get paginated results with outfit count
        var schools = await schoolsQuery
            .OrderBy(s => s.SchoolName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SchoolDto(
                s.Id,
                s.SchoolName,
                s.LogoURL,
                s.ContactInfo,
                s.Outfits.Count(o => o.IsAvailable)
            ))
            .ToListAsync(ct);

        return new SchoolListResponse(schools, totalCount, query.Page, query.PageSize);
    }
}

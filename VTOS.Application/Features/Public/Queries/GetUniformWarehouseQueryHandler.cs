using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Public.Queries;

public class GetUniformWarehouseQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetUniformWarehouseQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<UniformWarehouseResponse> HandleAsync(GetUniformWarehouseQuery query, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var activeCampaigns = await _db.SemesterPublications
            .AsNoTracking()
            .Include(sp => sp.School)
            .Where(sp => sp.Status == SemesterPublicationStatus.Active && sp.StartDate <= now)
            .OrderBy(sp => sp.EndDate)
            .Take(4)
            .Select(sp => new CampaignSummaryDto(
                sp.Id,
                sp.Semester + " " + sp.AcademicYear,
                sp.SchoolID,
                sp.School.SchoolName,
                sp.School.LogoURL,
                sp.StartDate,
                sp.EndDate,
                sp.Status.ToString()
            ))
            .ToListAsync(ct);

        var featuredOutfits = await _db.Outfits
            .AsNoTracking()
            .Include(o => o.School)
            .Where(o => !o.IsDeleted && o.IsAvailable)
            .OrderByDescending(o => o.Price)
            .Take(4)
            .Select(o => new FeaturedOutfitDto(
                o.Id,
                o.OutfitName,
                o.Price,
                o.MainImageURL,
                o.School.SchoolName,
                o.SchoolID,
                0
            ))
            .ToListAsync(ct);

        var allOutfitsQuery = _db.Outfits
            .AsNoTracking()
            .Include(o => o.School)
            .Where(o => !o.IsDeleted && o.IsAvailable);

        var totalCount = await allOutfitsQuery.CountAsync(ct);

        var allOutfits = await allOutfitsQuery
            .OrderBy(o => o.OutfitName)
            .Take(query.PageSize)
            .Select(o => new UniformSearchResult
            {
                Id = o.Id,
                OutfitName = o.OutfitName,
                MainImageUrl = o.MainImageURL,
                Price = o.Price,
                SchoolName = o.School.SchoolName,
                SchoolId = o.SchoolID
            })
            .ToListAsync(ct);

        return new UniformWarehouseResponse(
            activeCampaigns,
            featuredOutfits,
            allOutfits,
            totalCount
        );
    }
}

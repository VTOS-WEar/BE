using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Public.Queries;

public class GetUniformWarehouseQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetUniformWarehouseQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<UniformWarehouseResponse> HandleAsync(GetUniformWarehouseQuery query, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // 1. Active Campaigns
        var activeCampaigns = await _db.Campaigns
            .AsNoTracking()
            .Include(c => c.School)
            .Where(c => c.Status == CampaignStatus.Active && c.StartDate <= now && c.EndDate >= now)
            .OrderBy(c => c.EndDate)
            .Take(4)
            .Select(c => new CampaignSummaryDto(
                c.Id,
                c.CampaignName,
                c.SchoolID,
                c.School.SchoolName,
                c.School.LogoURL,
                c.StartDate,
                c.EndDate,
                c.Status.ToString()
            ))
            .ToListAsync(ct);

        // 2. Featured Outfits (Top rated or Newest available)
        var featuredOutfits = await _db.Outfits
            .AsNoTracking()
            .Include(o => o.School)
            .Where(o => !o.IsDeleted && o.IsAvailable)
            .OrderByDescending(o => o.Price) // Simple heuristic: expensive/premium
            .Take(4)
            .Select(o => new FeaturedOutfitDto(
                o.Id,
                o.OutfitName,
                o.Price,
                o.MainImageURL,
                o.School.SchoolName,
                o.SchoolID,
                0 // Placeholder for rating if not available in current schema
            ))
            .ToListAsync(ct);

        // 3. All Outfits
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

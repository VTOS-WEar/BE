using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetCampaignSelectedSizesQuery(Guid UserId, Guid CampaignId);

public record OutfitSizeBreakdownDto(string Size, int Count);

public record CampaignOutfitSizesDto(
    Guid OutfitId,
    string OutfitName,
    IReadOnlyList<OutfitSizeBreakdownDto> SizeBreakdown,
    int TotalQuantity
);

public interface IGetCampaignSelectedSizesQueryHandler
{
    Task<Result<IReadOnlyList<CampaignOutfitSizesDto>>> HandleAsync(GetCampaignSelectedSizesQuery query, CancellationToken ct = default);
}

public class GetCampaignSelectedSizesQueryHandler : IGetCampaignSelectedSizesQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetCampaignSelectedSizesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CampaignOutfitSizesDto>>> HandleAsync(GetCampaignSelectedSizesQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<IReadOnlyList<CampaignOutfitSizesDto>>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var campaign = await _db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == schoolMgr.SchoolID, ct);
        if (campaign == null)
            return Result<IReadOnlyList<CampaignOutfitSizesDto>>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        var orderItems = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.ProductVariant).ThenInclude(pv => pv.Outfit)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.CampaignID == query.CampaignId)
            .ToListAsync(ct);

        var result = orderItems
            .GroupBy(oi => new { oi.ProductVariant.OutfitID, oi.ProductVariant.Outfit.OutfitName })
            .Select(g => new CampaignOutfitSizesDto(
                g.Key.OutfitID,
                g.Key.OutfitName,
                g.GroupBy(x => x.SizeOrdered)
                 .OrderBy(sg => sg.Key)
                 .Select(sg => new OutfitSizeBreakdownDto(sg.Key, sg.Sum(x => x.Quantity)))
                 .ToList(),
                g.Sum(x => x.Quantity)
            ))
            .ToList();

        return Result<IReadOnlyList<CampaignOutfitSizesDto>>.Success(result);
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetCampaignTotalQuantityQuery(Guid UserId, Guid CampaignId);

public record SizeQuantityDto(string Size, int Quantity);

public record OutfitQuantityDto(
    Guid OutfitId,
    string OutfitName,
    IReadOnlyList<SizeQuantityDto> BySize,
    int TotalQuantity
);

public record CampaignTotalQuantityDto(
    Guid CampaignId,
    string CampaignName,
    int GrandTotal,
    IReadOnlyList<OutfitQuantityDto> Outfits
);

public interface IGetCampaignTotalQuantityQueryHandler
{
    Task<Result<CampaignTotalQuantityDto>> HandleAsync(GetCampaignTotalQuantityQuery query, CancellationToken ct = default);
}

public class GetCampaignTotalQuantityQueryHandler : IGetCampaignTotalQuantityQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetCampaignTotalQuantityQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<CampaignTotalQuantityDto>> HandleAsync(GetCampaignTotalQuantityQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<CampaignTotalQuantityDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var campaign = await _db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == user.SchoolID.Value, ct);
        if (campaign == null)
            return Result<CampaignTotalQuantityDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        var items = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.ProductVariant).ThenInclude(pv => pv.Outfit)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.CampaignID == query.CampaignId)
            .ToListAsync(ct);

        var outfits = items
            .GroupBy(oi => new { oi.ProductVariant.OutfitID, oi.ProductVariant.Outfit.OutfitName })
            .Select(g => new OutfitQuantityDto(
                g.Key.OutfitID,
                g.Key.OutfitName,
                g.GroupBy(x => x.SizeOrdered).OrderBy(s => s.Key)
                 .Select(s => new SizeQuantityDto(s.Key, s.Sum(x => x.Quantity))).ToList(),
                g.Sum(x => x.Quantity)
            ))
            .ToList();

        return Result<CampaignTotalQuantityDto>.Success(new CampaignTotalQuantityDto(
            campaign.Id, campaign.CampaignName, items.Sum(x => x.Quantity), outfits
        ));
    }
}

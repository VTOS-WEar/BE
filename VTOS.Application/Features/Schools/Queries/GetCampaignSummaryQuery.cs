using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

public record GetCampaignSummaryQuery(Guid UserId, Guid CampaignId);

public record OutfitSummaryDto(
    Guid OutfitId,
    string OutfitName,
    int TotalOrdered,
    decimal TotalRevenue
);

public record CampaignSummaryDto(
    Guid CampaignId,
    string CampaignName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    int TotalOrders,
    int TotalItemsOrdered,
    decimal TotalRevenue,
    IReadOnlyList<OutfitSummaryDto> OutfitSummaries
);

public interface IGetCampaignSummaryQueryHandler
{
    Task<Result<CampaignSummaryDto>> HandleAsync(GetCampaignSummaryQuery query, CancellationToken ct = default);
}

public class GetCampaignSummaryQueryHandler : IGetCampaignSummaryQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetCampaignSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<CampaignSummaryDto>> HandleAsync(GetCampaignSummaryQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user?.SchoolID == null)
            return Result<CampaignSummaryDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var campaign = await _db.Campaigns
            .AsNoTracking()
            .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Outfit)
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == user.SchoolID.Value, ct);

        if (campaign == null)
            return Result<CampaignSummaryDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        var allItems = campaign.Orders.SelectMany(o => o.OrderItems).ToList();

        var outfitSummaries = allItems
            .GroupBy(oi => new { oi.ProductVariant.OutfitID, oi.ProductVariant.Outfit.OutfitName })
            .Select(g => new OutfitSummaryDto(
                g.Key.OutfitID,
                g.Key.OutfitName,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.Quantity * x.UnitPrice)
            ))
            .ToList();

        return Result<CampaignSummaryDto>.Success(new CampaignSummaryDto(
            campaign.Id, campaign.CampaignName, campaign.Status.ToString(),
            campaign.StartDate, campaign.EndDate,
            campaign.Orders.Count,
            allItems.Sum(x => x.Quantity),
            allItems.Sum(x => x.Quantity * x.UnitPrice),
            outfitSummaries
        ));
    }
}

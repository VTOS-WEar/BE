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
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.SchoolID == user.SchoolID.Value, ct);

        if (campaign == null)
            return Result<CampaignSummaryDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        // Count orders separately (avoids loading Order entity with missing CancelReason column)
        var totalOrders = await _db.Orders
            .AsNoTracking()
            .CountAsync(o => o.CampaignID == query.CampaignId, ct);

        // Get order items via OrderItems → join Orders, avoiding full Order entity load
        var outfitSummaries = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.CampaignID == query.CampaignId)
            .GroupBy(oi => new { oi.ProductVariant.OutfitID, oi.ProductVariant.Outfit.OutfitName })
            .Select(g => new OutfitSummaryDto(
                g.Key.OutfitID,
                g.Key.OutfitName,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.Quantity * x.UnitPrice)
            ))
            .ToListAsync(ct);

        var totalItemsOrdered = outfitSummaries.Sum(s => s.TotalOrdered);
        var totalRevenue = outfitSummaries.Sum(s => s.TotalRevenue);

        return Result<CampaignSummaryDto>.Success(new CampaignSummaryDto(
            campaign.Id, campaign.CampaignName, campaign.Status.ToString(),
            campaign.StartDate, campaign.EndDate,
            totalOrders, totalItemsOrdered, totalRevenue,
            outfitSummaries
        ));
    }
}
